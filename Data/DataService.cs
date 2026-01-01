// ---------------------------------------------------------------------------------------------------------------------
// DataService.cs (base) – corrected drop-in
//
// Fixes / Adds:
// ✅ Keeps: PRAGMAs (FK/WAL), user_version, schema create, index
// ✅ Fixes: removes duplicate EnsureInitThen overloads (keeps the CT versions only)
// ✅ Adds: GetNextFutureVisitForStudentAsync(studentId) to support UX:
//          Tap student => edit existing future visit if present; otherwise schedule new.
// ✅ Uses EnsureInitThen wrappers so callers don't have to remember InitializeAsync()
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using SQLite;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        private const string DbFileName = "ministrytracker.db3";

        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _initialized;

        // Internal property used by all query/command methods once initialized.
        private SQLiteAsyncConnection Db =>
            _database ?? throw new InvalidOperationException("Database not initialized. Call InitializeAsync() first.");

        /// <summary>
        /// Create the SQLite connection and the database tables once.
        /// Safe to call multiple times; subsequent calls return immediately.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized) return;

            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_initialized) return;

                var path = Path.Combine(FileSystem.AppDataDirectory, DbFileName);

                _database = new SQLiteAsyncConnection(
                    path,
                    SQLiteOpenFlags.ReadWrite |
                    SQLiteOpenFlags.Create |
                    SQLiteOpenFlags.SharedCache);

                // ---- PRAGMAs: do once per connection ----------------------------------
                try
                {
                    // Enforce FKs (SQLite default is OFF)
                    _ = await _database.ExecuteScalarAsync<long>("PRAGMA foreign_keys = ON;").ConfigureAwait(false);

                    // WAL for better concurrency on mobile; returns "wal"
                    _ = await _database.ExecuteScalarAsync<string>("PRAGMA journal_mode = WAL;").ConfigureAwait(false);

                    // Reasonable durability/perf
                    _ = await _database.ExecuteScalarAsync<long>("PRAGMA synchronous = NORMAL;").ConfigureAwait(false);

                    // NOTE:
                    // If you want a busy timeout, sqlite-net-pcl doesn't expose it directly via a PRAGMA
                    // consistently across platforms. We can add one later if needed using ExecuteAsync.
                    // Example: PRAGMA busy_timeout = 5000;
                }
                catch (SQLiteException)
                {
                    // Don't fail app startup over a PRAGMA; log if you have logging
                }

                // ---- Schema: create tables & indexes -----------------------------------
                await Db.CreateTableAsync<Models.Student>().ConfigureAwait(false);
                await Db.CreateTableAsync<Models.Visit>().ConfigureAwait(false);

                // Supports fast "next visit" lookups by student+date
                await Db.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_Visit_StudentDate ON Visit(StudentId, ScheduledDateTime)"
                ).ConfigureAwait(false);

                // ---- Versioning hook (for future migrations) ---------------------------
                await _database.ExecuteAsync("PRAGMA user_version = 1;").ConfigureAwait(false);

                _initialized = true;
            }
            finally
            {
                _gate.Release();
            }
        }

        /// <summary>
        /// Returns the fully-qualified path to the database file (useful for logs/support).
        /// </summary>
        public string GetDatabasePath() =>
            Path.Combine(FileSystem.AppDataDirectory, DbFileName);

        // -------------------------------------------------------------------------------------------------------------
        // EnsureInitThen helpers
        //
        // WHY:
        // - Centralize "make sure DB is initialized" so every public DB method is safe.
        // - Keeps calling code (VMs) simple and less error-prone.
        //
        // NOTE:
        // - We keep ONLY the CancellationToken versions to avoid duplicate signatures.
        // -------------------------------------------------------------------------------------------------------------
        private async Task<T> EnsureInitThen<T>(Func<Task<T>> work, CancellationToken ct = default)
        {
            if (!_initialized) await InitializeAsync().ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return await work().ConfigureAwait(false);
        }

        private async Task EnsureInitThen(Func<Task> work, CancellationToken ct = default)
        {
            if (!_initialized) await InitializeAsync().ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            await work().ConfigureAwait(false);
        }

        // -------------------------------------------------------------------------------------------------------------
        // Visits – minimal query to support the "Tap = Next Visit" UX
        // -------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the next scheduled (future) visit for a student, or null if none exist.
        ///
        /// UX RULE SUPPORTED:
        /// - Tap student => edit existing future visit if present
        /// - Otherwise tap => schedule a new visit
        ///
        /// PERFORMANCE:
        /// - Efficient because of IX_Visit_StudentDate (StudentId, ScheduledDateTime).
        /// </summary>
        public Task<Models.Visit?> GetNextFutureVisitForStudentAsync(int studentId, CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                var now = DateTime.Now; // consistent with typical local-time scheduling UX

                return await Db.Table<Models.Visit>()
                    .Where(v => v.StudentId == studentId && v.ScheduledDateTime > now)
                    .OrderBy(v => v.ScheduledDateTime)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }, ct);
        }
    }
}
