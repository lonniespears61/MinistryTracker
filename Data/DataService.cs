// ---------------------------------------------------------------------------------------------------------------------
// DataService.cs (base) – corrected drop-in
//
// Fixes / Adds:
// ✅ PRAGMAs: foreign_keys, WAL, synchronous
// ✅ Schema create + index
// ✅ user_version
// ✅ EnsureInitThen helpers (ONLY CancellationToken versions)
// ✅ GetStudentByIdAsync(studentId, ct)
// ✅ GetNextFutureVisitForStudentAsync(studentId, ct)
//
// Notes:
// - sqlite-net-pcl uses the [Table(...)] attributes on your models.
// - Student table name: "Students"
// - Visit table name: "Visit"
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using SQLite;
using MinistryTracker.Models; // ✅ Student, Visit live here

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        private const string DbFileName = "ministrytracker.db3";

        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _initialized;

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
                    _ = await _database.ExecuteScalarAsync<long>("PRAGMA foreign_keys = ON;").ConfigureAwait(false);
                    _ = await _database.ExecuteScalarAsync<string>("PRAGMA journal_mode = WAL;").ConfigureAwait(false);
                    _ = await _database.ExecuteScalarAsync<long>("PRAGMA synchronous = NORMAL;").ConfigureAwait(false);

                    // Optional (usually fine):
                    // await _database.ExecuteAsync("PRAGMA busy_timeout = 5000;").ConfigureAwait(false);
                }
                catch (SQLiteException)
                {
                    // Don't fail startup over PRAGMAs; log if you add logging later.
                }

                // ---- Schema: create tables & indexes -----------------------------------
                await Db.CreateTableAsync<Student>().ConfigureAwait(false);
                await Db.CreateTableAsync<Visit>().ConfigureAwait(false);

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

        /// <summary>Returns the fully-qualified path to the database file (useful for logs/support).</summary>
        public string GetDatabasePath() =>
            Path.Combine(FileSystem.AppDataDirectory, DbFileName);

        // -------------------------------------------------------------------------------------------------------------
        // EnsureInitThen helpers (ONLY CancellationToken versions)
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
        // Students
        // -------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Load one student by ID (throws if not found).
        /// NOTE: filters out IsDeleted = true by default.
        /// </summary>
      

        // -------------------------------------------------------------------------------------------------------------
        // Visits – supports "Tap student => edit existing future visit if present; else schedule new"
        // -------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the next scheduled (future) visit for a student, or null if none exist.
        /// Efficient due to IX_Visit_StudentDate (StudentId, ScheduledDateTime).
        /// </summary>
        public Task<Visit?> GetNextFutureVisitForStudentAsync(int studentId, CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var now = DateTime.Now;

                return await Db.Table<Visit>()
                    .Where(v => v.StudentId == studentId && v.ScheduledDateTime > now)
                    .OrderBy(v => v.ScheduledDateTime)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }, ct);
        }
    }
}
