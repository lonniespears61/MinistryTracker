// ---------------------------------------------------------------------------------------------------------------------
// DataService.Diagnostics.cs
// Diagnostics + maintenance helpers for the SQLite DB.
// - GetDbHealthAsync(): quick health snapshot (exists/size/integrity/tables/counts)
// - ReseedAsync(): inserts small, deterministic sample data (only if empty)
// - ResetDatabaseAsync(): clears local DB (beta-safe), then recreates schema
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
        // Helper for table-name query (moved OUT of the method; C# doesn't allow local type declarations in methods)
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

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"DB Path: {Path}");
                sb.AppendLine($"Exists / Bytes: {FileExists} / {FileBytes}");
                sb.AppendLine($"Integrity: {Integrity}");
                sb.AppendLine($"Tables: {TablesCsv}");
                sb.AppendLine($"Students: {StudentCount}   Visits: {VisitCount}");
                return sb.ToString();
            }
        }

        /// <summary>Quick on-device status: file exists/size, integrity, tables, row counts.</summary>
        public async Task<DbHealth> GetDbHealthAsync()
        {
            await InitializeAsync().ConfigureAwait(false);

            var dbPath = GetDatabasePath();
            var exists = File.Exists(dbPath);
            var size = exists ? new FileInfo(dbPath).Length : 0;

            using var ro = new SQLiteConnection(dbPath, SQLiteOpenFlags.ReadOnly);

            var integrity = ro.ExecuteScalar<string>("PRAGMA integrity_check;");
            var tables = ro.Query<_NameRow>("SELECT name AS V FROM sqlite_master WHERE type='table' ORDER BY name;");
            var tablesCsv = string.Join(",", tables.Select(t => t.V));

            int students = 0, visits = 0;
            try { students = ro.ExecuteScalar<int>("SELECT COUNT(*) FROM Student;"); } catch { }
            try { visits = ro.ExecuteScalar<int>("SELECT COUNT(*) FROM Visit;"); } catch { }

            return new DbHealth
            {
                Path = dbPath,
                FileExists = exists,
                FileBytes = size,
                Integrity = integrity,
                TablesCsv = tablesCsv,
                StudentCount = students,
                VisitCount = visits
            };
        }

        /// <summary>Seed a small, deterministic set of data (only when empty). Safe to call repeatedly.</summary>
        public async Task ReseedAsync()
        {
            await InitializeAsync().ConfigureAwait(false);

            // Avoid EnsureInitThen() here to prevent overload ambiguity across partials
            var existing = await Db.Table<Models.Student>().CountAsync().ConfigureAwait(false);
            if (existing > 0) return; // already has data

            // Minimal fields to avoid model mismatches; adjust to your actual model as needed
            var s1 = new Models.Student
            {
                Name = "Jane Doe",
                Status = Models.Enums.StudentStatus.Active
            };
            var s2 = new Models.Student
            {
                Name = "John Smith",
                Status = Models.Enums.StudentStatus.Active
            };

            await Db.InsertAllAsync(new[] { s1, s2 }).ConfigureAwait(false);

            var visits = new[]
            {
                new Models.Visit { StudentId = s1.StudentId, ScheduledDateTime = DateTime.Today.AddDays(1), Notes = "Initial visit" },
                new Models.Visit { StudentId = s2.StudentId, ScheduledDateTime = DateTime.Today.AddDays(2), Notes = "Follow-up"   },
            };
            await Db.InsertAllAsync(visits).ConfigureAwait(false);
        }

        /// <summary>Removes the DB file and recreates a clean, empty DB. Useful for beta testers.</summary>
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
    }
}
