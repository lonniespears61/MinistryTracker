// ---------------------------------------------------------------------------------------------------------------------
// DataService.Diagnostics.cs
// Health snapshot + maintenance + dev seeding façade.
// Uses EnsureInitThen(...) where touching ORM (counts). Uses raw connection for PRAGMA + table scan.
// Fixes table naming by reading sqlite-net mappings (e.g., Student -> "Students").
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SQLite;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        // Helper for table-name query when listing sqlite_master
        private sealed class _NameRow { public string V { get; set; } = ""; }

        public sealed class DbHealth
        {
            public string Path { get; init; } = "";
            public bool FileExists { get; init; }
            public long FileBytes { get; init; }
            public string Integrity { get; init; } = "";
            public string TablesCsv { get; init; } = "";
            public int StudentCount { get; init; }
            public int VisitCount { get; init; }

            // Rows in Visit that have no matching Student (based on StudentId)
            public int OrphanVisitCount { get; init; }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"DB Path: {Path}");
                sb.AppendLine($"Exists / Bytes: {FileExists} / {FileBytes}");
                sb.AppendLine($"Integrity: {Integrity}");
                sb.AppendLine($"Tables: {TablesCsv}");
                sb.AppendLine($"Students: {StudentCount}   Visits: {VisitCount}");
                sb.AppendLine($"Orphaned Visits: {OrphanVisitCount}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Quick on-device status:
        /// - counts via ORM (respects table mapping/attributes)
        /// - optional PRAGMA integrity_check (deep mode)
        /// - list of tables
        /// - orphaned visits (raw LEFT JOIN)
        /// </summary>
        public async Task<DbHealth> GetDbHealthAsync(bool deep = false, CancellationToken ct = default)
        {
            // ORM counts (mapped table names, async connection)
            var ormStudents = await EnsureInitThen(() => Db.Table<Models.Student>().CountAsync(), ct).ConfigureAwait(false);
            var ormVisits = await EnsureInitThen(() => Db.Table<Models.Visit>().CountAsync(), ct).ConfigureAwait(false);

            // File info
            var dbPath = GetDatabasePath();
            var exists = File.Exists(dbPath);
            var size = exists ? new FileInfo(dbPath).Length : 0;

            // Raw-only bits: PRAGMA + sqlite_master + orphan join (off UI thread)
            return await Task.Run(() =>
            {
                using var ro = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);
                ro.BusyTimeout = TimeSpan.FromSeconds(2);

                // Read sqlite-net mappings to get the actual table names
                var studentTable = ro.GetMapping(typeof(Models.Student)).TableName; // e.g., "Students"
                var visitTable = ro.GetMapping(typeof(Models.Visit)).TableName;   // likely "Visit"

                var integrity = deep
                    ? ro.ExecuteScalar<string>("PRAGMA integrity_check;")
                    : "SKIPPED (fast mode)";

                var tables = ro.Query<_NameRow>("SELECT name AS V FROM sqlite_master WHERE type='table' ORDER BY name;");
                var tablesCsv = string.Join(",", tables.Select(t => t.V));

                int orphanVisits = 0;
                try
                {
                    orphanVisits = ro.ExecuteScalar<int>($@"
                        SELECT COUNT(*)
                        FROM ""{visitTable}"" v
                        LEFT JOIN ""{studentTable}"" s ON v.StudentId = s.StudentId
                        WHERE s.StudentId IS NULL;");
                }
                catch { /* best effort on dev devices */ }

                return new DbHealth
                {
                    Path = dbPath,
                    FileExists = exists,
                    FileBytes = size,
                    Integrity = integrity,
                    TablesCsv = tablesCsv,
                    StudentCount = ormStudents,
                    VisitCount = ormVisits,
                    OrphanVisitCount = orphanVisits
                };
            }, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes the DB file and recreates a clean, empty DB. Useful for beta testers and “start fresh” flows.
        /// </summary>
        public async Task ResetDatabaseAsync(CancellationToken ct = default)
        {
            // Ensure nothing else is using the file while we reset
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_database is not null)
                {
                    try { _database.GetConnection().Close(); } catch { /* best effort */ }
                    _database = null;
                }
                _initialized = false;

                var path = GetDatabasePath();
                if (File.Exists(path))
                {
                    try { File.Delete(path); }
                    catch
                    {
                        // Fallback: if delete fails due to a lingering handle, drop rows instead
                        await EnsureInitThen(async () =>
                        {
                            try { await Db.DeleteAllAsync<Models.Visit>().ConfigureAwait(false); } catch { }
                            try { await Db.DeleteAllAsync<Models.Student>().ConfigureAwait(false); } catch { }
                        }, ct).ConfigureAwait(false);
                        return;
                    }
                }

                // Recreate fresh DB/tables/PRAGMAs via normal init
                await InitializeAsync().ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Flush WAL so newly opened read-only connections immediately see latest data.
        /// </summary>
        public Task ForceWalCheckpointAsync(CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                try { _ = await Db.ExecuteScalarAsync<long>("PRAGMA wal_checkpoint(FULL);").ConfigureAwait(false); }
                catch { /* best effort */ }
            }, ct);

        // ---------------------------------------------------------------------------------------------
        // Diagnostics façade → Seeding (so Settings can trigger seeding through diagnostics)
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Convenience wrapper so UI can ask the Diagnostics surface to seed dev/demo data.
        /// Implementation lives in DataService.Seeding.cs.
        /// </summary>
        public Task SeedDevDataFromDiagnosticsAsync(bool force = false, CancellationToken ct = default)
            => SeedDevDataAsync(force, ct);

        /// <summary>
        /// Handy combo for Settings: reset DB then re-seed demo data in one tap (and checkpoint).
        /// </summary>
        public async Task ResetAndSeedFromDiagnosticsAsync(CancellationToken ct = default)
        {
            await ResetDatabaseAsync(ct).ConfigureAwait(false);
            await SeedDevDataAsync(force: true, ct).ConfigureAwait(false);
            await ForceWalCheckpointAsync(ct).ConfigureAwait(false);
        }
    }
}
