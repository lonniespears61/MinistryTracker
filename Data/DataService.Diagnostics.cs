// ---------------------------------------------------------------------------------------------------------------------
// DataService.Diagnostics.cs (DROP-IN)
//
// PURPOSE
// - Diagnostics helpers (safe wrappers used by Settings/Diagnostics UI)
// - Keeps destructive or debug-only helpers grouped away from core CRUD
//
// CHANGE NOTES (04/12/2026)
// - Added schema version visibility (DB vs App)
// - Added migration detection flag
// - Extended schema health report to include version mismatch info
// - Added full database reset methods (delete DB file)
// - Hardened full reset so the live SQLite connection is closed before file delete
// - Fixed deadlock by reinitializing AFTER releasing the shared DB gate
// - Renamed local file-delete helper to avoid partial-class ambiguity
//
// IMPORTANT
// - This file MUST NOT re-define SeedDemoDataAsync.
// - It only CALLS it (implemented in DataService.Seeding.cs).
// ---------------------------------------------------------------------------------------------------------------------

using MinistryTracker.Models;
using SQLite;
using System.IO;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        public Task RunSeedDemoDataAsync(CancellationToken ct = default)
            => SeedDemoDataAsync(ct);

        public Task ResetDatabaseAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                await Db.DeleteAllAsync<Visit>().ConfigureAwait(false);
                await Db.DeleteAllAsync<Student>().ConfigureAwait(false);

            }, ct);
        }

        // -----------------------------------------------------------------------------------------------------------------
        // FULL RESET
        // -----------------------------------------------------------------------------------------------------------------
        // WHY:
        // - Row deletes are not enough when schema must be rebuilt
        // - Closing the live connection first avoids "reset looked successful but old data is still there"
        // - WAL/SHM sidecars must also be removed so the next startup is a truly clean DB
        // - InitializeAsync must happen AFTER releasing _gate or we deadlock on the same lock

        public async Task FullResetDatabaseAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var path = GetDatabasePath();
            var walPath = path + "-wal";
            var shmPath = path + "-shm";

            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ct.ThrowIfCancellationRequested();

                if (_database is not null)
                {
                    try
                    {
                        await _database.CloseAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        // Best effort. We still reset pool + clear local state below.
                    }
                }

                SQLiteAsyncConnection.ResetPool();

                _database = null;
                _initialized = false;

                DeleteFileIfExists(path);
                DeleteFileIfExists(walPath);
                DeleteFileIfExists(shmPath);
            }
            finally
            {
                _gate.Release();
            }

            await InitializeAsync().ConfigureAwait(false);
        }

        public async Task FullResetDatabaseAndSeedAsync(CancellationToken ct = default)
        {
            await FullResetDatabaseAsync(ct).ConfigureAwait(false);
            await SeedDemoDataAsync(ct).ConfigureAwait(false);
        }

        private static void DeleteFileIfExists(string path)
        {
            if (!File.Exists(path))
                return;

            File.Delete(path);
        }

        public Task<(string DbPath, int StudentCount, int VisitCount)> GetDatabaseHealthAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var students = await Db.Table<Student>().CountAsync().ConfigureAwait(false);
                var visits = await Db.Table<Visit>().CountAsync().ConfigureAwait(false);

                return (GetDatabasePath(), students, visits);
            }, ct);
        }

        // -----------------------------------------------------------------------------------------------------------------
        // SCHEMA DIAGNOSTICS
        // -----------------------------------------------------------------------------------------------------------------

        public sealed record DatabaseSchemaHealth(
            string DbPath,
            int DbSchemaVersion,
            int AppSchemaVersion,
            bool MigrationRequired,
            IReadOnlyList<TableSchemaHealth> Tables,
            string ReportText,
            DateTime GeneratedAtUtc
        );

        public sealed record TableSchemaHealth(
            string Name,
            int RowCount,
            IReadOnlyList<ColumnSchemaHealth> Columns
        );

        public sealed record ColumnSchemaHealth(
            int Ordinal,
            string Name,
            string Type,
            bool IsNotNull,
            bool IsPrimaryKey,
            string? DefaultValue
        );

        private sealed class SqliteMasterRow
        {
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
        }

        private sealed class PragmaTableInfoRow
        {
            public int cid { get; set; }
            public string name { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public int notnull { get; set; }
            public string? dflt_value { get; set; }
            public int pk { get; set; }
        }

        public Task<DatabaseSchemaHealth> GetDatabaseSchemaHealthAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var dbPath = GetDatabasePath();

                var dbVersion = await Db.ExecuteScalarAsync<int>("PRAGMA user_version;").ConfigureAwait(false);
                var appVersion = GetAppSchemaVersion();

                var migrationRequired = dbVersion < appVersion;

                var tables = await Db.QueryAsync<SqliteMasterRow>(
                    "SELECT name, type FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;"
                ).ConfigureAwait(false);

                var tableHealth = new System.Collections.Generic.List<TableSchemaHealth>();

                foreach (var t in tables)
                {
                    var tableName = t.name;

                    var cols = await Db.QueryAsync<PragmaTableInfoRow>(
                        $"PRAGMA table_info(\"{tableName}\");"
                    ).ConfigureAwait(false);

                    var mappedCols = cols
                        .OrderBy(c => c.cid)
                        .Select(c => new ColumnSchemaHealth(
                            c.cid,
                            c.name,
                            c.type,
                            c.notnull == 1,
                            c.pk == 1,
                            c.dflt_value
                        ))
                        .ToList();

                    var rowCount = await Db.ExecuteScalarAsync<int>(
                        $"SELECT COUNT(*) FROM \"{tableName}\";"
                    ).ConfigureAwait(false);

                    tableHealth.Add(new TableSchemaHealth(
                        tableName,
                        rowCount,
                        mappedCols
                    ));
                }

                var report = BuildSchemaReportText(
                    dbPath,
                    dbVersion,
                    appVersion,
                    migrationRequired,
                    tableHealth
                );

                return new DatabaseSchemaHealth(
                    dbPath,
                    dbVersion,
                    appVersion,
                    migrationRequired,
                    tableHealth,
                    report,
                    DateTime.UtcNow
                );
            }, ct);
        }

        private static string BuildSchemaReportText(
            string dbPath,
            int dbVersion,
            int appVersion,
            bool migrationRequired,
            IReadOnlyList<TableSchemaHealth> tables)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== DB Schema Health ===");
            sb.AppendLine($"DB Path: {dbPath}");
            sb.AppendLine($"DB Schema Version: {dbVersion}");
            sb.AppendLine($"App Schema Version: {appVersion}");
            sb.AppendLine($"Migration Required: {migrationRequired}");
            sb.AppendLine($"Generated (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            foreach (var t in tables.OrderBy(x => x.Name))
            {
                sb.AppendLine($"[Table] {t.Name} (rows: {t.RowCount})");

                foreach (var c in t.Columns.OrderBy(x => x.Ordinal))
                {
                    sb.Append("  - ");
                    sb.Append(c.Name);
                    sb.Append(' ');
                    sb.Append(c.Type);

                    if (c.IsPrimaryKey) sb.Append(" PK");
                    if (c.IsNotNull) sb.Append(" NOT NULL");
                    if (c.DefaultValue is not null) sb.Append($" DEFAULT {c.DefaultValue}");

                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}