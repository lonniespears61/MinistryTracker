// ---------------------------------------------------------------------------------------------------------------------
// DataService.Diagnostics.cs (DROP-IN)
//
// PURPOSE:
// - Diagnostics helpers (safe wrappers used by Settings/Diagnostics UI)
// - Keeps destructive or debug-only helpers grouped away from core CRUD
//
// IMPORTANT:
// - This file MUST NOT re-define SeedDemoDataAsync.
//   It only CALLS it (implemented in DataService.Seeding.cs).
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using MinistryTracker.Models;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        /// <summary>
        /// Runs demo seeding (idempotent).
        /// Safe to expose behind a "Seed Demo Data" button.
        /// </summary>
        public Task RunSeedDemoDataAsync(CancellationToken ct = default)
            => SeedDemoDataAsync(ct);

        /// <summary>
        /// Deletes all rows from Student + Visit tables (hard reset).
        /// Use behind a "Reset DB" button.
        /// </summary>
        public Task ResetDatabaseAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                // Delete child rows first to avoid FK issues.
                await Db.DeleteAllAsync<Visit>().ConfigureAwait(false);
                await Db.DeleteAllAsync<Student>().ConfigureAwait(false);

                // Optional: vacuum can be slow on mobile; keep it off unless needed.
                // await Db.ExecuteAsync("VACUUM;").ConfigureAwait(false);

            }, ct);
        }

        /// <summary>
        /// Quick health check you can display in UI.
        /// </summary>
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

        /// <summary>
        /// Optional WAL checkpoint. Usually not needed; here for troubleshooting.
        /// </summary>
        public Task ForceWalCheckpointAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await Db.ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE);").ConfigureAwait(false);
                }
                catch (SQLiteException)
                {
                    // Ignore; not all platforms/configs support WAL the same way.
                }
            }, ct);
        }

        // -------------------------------------------------------------------------------------------------------------
        // Schema diagnostics (authoritative: reads SQLite's own metadata)
        // -------------------------------------------------------------------------------------------------------------

        public sealed record DatabaseSchemaHealth(
            string DbPath,
            int UserVersion,
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

        /// <summary>
        /// Authoritative schema snapshot:
        /// - PRAGMA user_version
        /// - All user tables
        /// - Column definitions per table (PRAGMA table_info)
        /// - Row counts per table
        ///
        /// Use this when debugging migrations / "does column exist?" questions.
        /// </summary>
        public Task<DatabaseSchemaHealth> GetDatabaseSchemaHealthAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var dbPath = GetDatabasePath();
                var userVersion = await Db.ExecuteScalarAsync<int>("PRAGMA user_version;").ConfigureAwait(false);

                // List all non-internal tables
                var tables = await Db.QueryAsync<SqliteMasterRow>(
                    "SELECT name, type FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;"
                ).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();

                var tableHealth = new System.Collections.Generic.List<TableSchemaHealth>();

                foreach (var t in tables)
                {
                    var tableName = t.name;

                    // Columns
                    var cols = await Db.QueryAsync<PragmaTableInfoRow>(
                        $"PRAGMA table_info({EscapeIdentifier(tableName)});"
                    ).ConfigureAwait(false);

                    var mappedCols = cols
                        .OrderBy(c => c.cid)
                        .Select(c => new ColumnSchemaHealth(
                            Ordinal: c.cid,
                            Name: c.name,
                            Type: c.type,
                            IsNotNull: c.notnull == 1,
                            IsPrimaryKey: c.pk == 1,
                            DefaultValue: c.dflt_value
                        ))
                        .ToList();

                    // Row count
                    var rowCount = await Db.ExecuteScalarAsync<int>(
                        $"SELECT COUNT(*) FROM {EscapeIdentifier(tableName)};"
                    ).ConfigureAwait(false);

                    tableHealth.Add(new TableSchemaHealth(
                        Name: tableName,
                        RowCount: rowCount,
                        Columns: mappedCols
                    ));

                    ct.ThrowIfCancellationRequested();
                }

                var report = BuildSchemaReportText(dbPath, userVersion, tableHealth);

                return new DatabaseSchemaHealth(
                    DbPath: dbPath,
                    UserVersion: userVersion,
                    Tables: tableHealth,
                    ReportText: report,
                    GeneratedAtUtc: DateTime.UtcNow
                );
            }, ct);
        }

        private static string BuildSchemaReportText(string dbPath, int userVersion, IReadOnlyList<TableSchemaHealth> tables)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== DB Schema Health ===");
            sb.AppendLine($"DB Path: {dbPath}");
            sb.AppendLine($"PRAGMA user_version: {userVersion}");
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

            // Helpful quick checks (expand later as needed)
            var students = tables.FirstOrDefault(t => string.Equals(t.Name, "Students", StringComparison.OrdinalIgnoreCase));
            if (students is not null)
            {
                var hasNotes = students.Columns.Any(c => string.Equals(c.Name, "Notes", StringComparison.OrdinalIgnoreCase));
                sb.AppendLine($"Students.Notes column present: {hasNotes}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// SQLite-safe identifier quoting.
        /// Uses double quotes; also escapes embedded quotes.
        /// </summary>
        private static string EscapeIdentifier(string identifier)
        {
            var safe = identifier.Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }

    }

}
