// ---------------------------------------------------------------------------------------------------------------------
// DataService.cs (base)
// Database connection + initialization + EnsureInitThen helpers.
// Keep this file boring: no Student/Visit feature queries here.
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using SQLite;
using MinistryTracker.Models;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        private const string DbFileName = "ministrytracker.db3";

        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _initialized;

        // NOTE: other partials should use Db (not _database directly).
        internal SQLiteAsyncConnection Db =>
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
                }
                catch (SQLiteException)
                {
                    // Don't fail startup over PRAGMAs; add logging later if desired.
                }

                // ---- Schema: create tables & indexes ----------------------------------
                await Db.CreateTableAsync<Student>().ConfigureAwait(false);
                await Db.CreateTableAsync<Visit>().ConfigureAwait(false);

                await Db.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_Visit_StudentDate ON Visit(StudentId, ScheduledDateTime)"
                ).ConfigureAwait(false);

                // ---- Versioning hook (for future migrations) --------------------------
                await ApplyMigrationsAsync(_database).ConfigureAwait(false);

                _initialized = true;
            }
            finally
            {
                _gate.Release();
            }
        }

        private const int CurrentSchemaVersion = 1;

        private static async Task ApplyMigrationsAsync(SQLiteAsyncConnection db)
        {
            // 0 means "no version set yet" (often a fresh DB)
            var version = await db.ExecuteScalarAsync<int>("PRAGMA user_version;").ConfigureAwait(false);

            // Fresh install path: tables already created above.
            // Set version once so future migrations have a baseline.
            if (version == 0)
            {
                await db.ExecuteAsync($"PRAGMA user_version = {CurrentSchemaVersion};").ConfigureAwait(false);
                return;
            }

            // Future upgrade path: migrate incrementally.
            // Keep this structure even while we're on v1.
            if (version < 1)
            {
                // In practice you won't see this (SQLite user_version starts at 0),
                // but keeping the shape makes future diffs clean.
                await db.ExecuteAsync("PRAGMA user_version = 1;").ConfigureAwait(false);
                version = 1;
            }

            // Example future slots (DO NOT implement yet)
            // if (version < 2) { await MigrateToV2Async(db).ConfigureAwait(false); await db.ExecuteAsync("PRAGMA user_version = 2;"); version = 2; }
            // if (version < 3) { await MigrateToV3Async(db).ConfigureAwait(false); await db.ExecuteAsync("PRAGMA user_version = 3;"); version = 3; }

            // Optional: sanity check (useful later)
            // if (version > CurrentSchemaVersion) { /* app older than DB; decide what to do */ }
        }

        /// <summary>Returns the fully-qualified path to the database file (useful for logs/support).</summary>
        public string GetDatabasePath() =>
            Path.Combine(FileSystem.AppDataDirectory, DbFileName);

        // -------------------------------------------------------------------------------------------------------------
        // EnsureInitThen helpers (CancellationToken versions)
        // -------------------------------------------------------------------------------------------------------------

        internal async Task<T> EnsureInitThen<T>(Func<Task<T>> work, CancellationToken ct = default)
        {
            if (!_initialized) await InitializeAsync().ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            return await work().ConfigureAwait(false);
        }

        internal async Task EnsureInitThen(Func<Task> work, CancellationToken ct = default)
        {
            if (!_initialized) await InitializeAsync().ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            await work().ConfigureAwait(false);
        }
    }
}
