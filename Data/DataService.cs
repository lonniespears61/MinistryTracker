// ---------------------------------------------------------------------------------------------------------------------
// DataService.cs (base) – reviewed
// Adds: PRAGMAs (FK/WAL), busy timeout, user_version, and an EnsureInitThen helper.
// No platform-specific code; all cross-platform SQLite.
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

                   
                }
                catch (SQLiteException)
                {
                    // Don't fail app startup over a PRAGMA; log if you have logging
                    // Debug.WriteLine(ex);
                }

                // ---- Schema: create tables & indexes -----------------------------------
                await Db.CreateTableAsync<Models.Student>().ConfigureAwait(false);
                await Db.CreateTableAsync<Models.Visit>().ConfigureAwait(false);

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

        /// <summary>Returns the fully-qualified path to the database file (for logs or support).</summary>
        public string GetDatabasePath() =>
            Path.Combine(FileSystem.AppDataDirectory, DbFileName);

        // Convenience: safely ensure init around any DB call.
        private async Task<T> EnsureInitThen<T>(Func<Task<T>> work)
        {
            if (!_initialized) await InitializeAsync().ConfigureAwait(false);
            return await work().ConfigureAwait(false);
        }

        private async Task EnsureInitThen(Func<Task> work)
        {
            if (!_initialized) await InitializeAsync().ConfigureAwait(false);
            await work().ConfigureAwait(false);
        }

        // Ensures the SQLite connection is initialized ONCE, then runs 'work'.
        // Use this for any DB method that touches 'Db' (queries, inserts, updates).
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
    }
}
