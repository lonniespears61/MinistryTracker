// ---------------------------------------------------------------------------------------------------------------------
// DataService.Diagnostics.cs
// Health snapshot + maintenance utilities for the local SQLite DB.
// Also exposes a tiny façade to trigger dev seeding (implemented in DataService.Seeding.cs).
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        // Helper for table-name query (C# does not allow local type declarations in methods)
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

            // NEW: shows dangling Visit rows without a Student
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
        /// Quick on-device status: file exists/size, integrity, table list, and row counts.
        /// </summary>
        public async Task<DbHealth> GetDbHealthAsync()
        {
            await InitializeAsync().ConfigureAwait(false);

            var dbPath = GetDatabasePath();
            var exists = File.Exists(dbPath);
            var size = exists ? new FileInfo(dbPath).Length : 0;

            // Run the synchronous SQLite work off the UI thread
            return await Task.Run(() =>
            {
                using var ro = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);

                var integrity = ro.ExecuteScalar<string>("PRAGMA integrity_check;");
                var tables = ro.Query<_NameRow>("SELECT name AS V FROM sqlite_master WHERE type='table' ORDER BY name;");
                var tablesCsv = string.Join(",", tables.Select(t => t.V));

                int students = 0, visits = 0, orphanVisits = 0;
                try { students = ro.ExecuteScalar<int>("SELECT COUNT(*) FROM Students;"); } catch { }
                try { visits = ro.ExecuteScalar<int>("SELECT COUNT(*) FROM Visit;"); } catch { }
                try
                {
                    orphanVisits = ro.ExecuteScalar<int>(
                        @"SELECT COUNT(*)
                          FROM Visit v
                          LEFT JOIN Student s ON v.StudentId = s.StudentId
                          WHERE s.StudentId IS NULL;");
                }
                catch { }

                return new DbHealth
                {
                    Path = dbPath,
                    FileExists = exists,
                    FileBytes = size,
                    Integrity = integrity,
                    TablesCsv = tablesCsv,
                    StudentCount = students,
                    VisitCount = visits,
                    OrphanVisitCount = orphanVisits
                };
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Removes the DB file and recreates a clean, empty DB. Useful for beta testers and “start fresh” flows.
        /// </summary>
        public async Task ResetDatabaseAsync()
        {
            // Ensure nothing else is using the file while we reset
            await _gate.WaitAsync().ConfigureAwait(false);
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
                        await InitializeAsync().ConfigureAwait(false);
                        try { await Db.DeleteAllAsync<Models.Visit>().ConfigureAwait(false); } catch { }
                        try { await Db.DeleteAllAsync<Models.Student>().ConfigureAwait(false); } catch { }
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

        // ---------------------------------------------------------------------------------------------
        // Diagnostics façade → Seeding (so Settings can “call diagnostics,” which delegates to seeding)
        // ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Convenience wrapper so UI can ask the Diagnostics surface to seed dev/demo data.
        /// Implementation lives in DataService.Seeding.cs.
        /// </summary>
        public Task SeedDevDataFromDiagnosticsAsync(bool force = false) => SeedDevDataAsync(force);

        /// <summary>
        /// Handy combo for Settings: reset DB then re-seed demo data in one tap.
        /// </summary>
        public async Task ResetAndSeedFromDiagnosticsAsync()
        {
            await ResetDatabaseAsync().ConfigureAwait(false);
            await SeedDevDataAsync(force: true).ConfigureAwait(false);
        }
    }
}
