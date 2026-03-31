// ---------------------------------------------------------------------------------------------------------------------
// DataService.cs (base)
//
// PURPOSE
// - Own the SQLite connection
// - Create tables
// - Apply migrations
// - Provide EnsureInitThen helpers used by the partial DataService files
//
// DESIGN RULES
// - Keep this file boring
// - No Student/Visit feature queries here
// - All table names must match the model [Table(...)] attributes
//
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

        /// <summary>
        /// Other partial classes should use Db, not _database directly.
        /// </summary>
        internal SQLiteAsyncConnection Db =>
            _database ?? throw new InvalidOperationException("Database not initialized. Call InitializeAsync() first.");

        /// <summary>
        /// Create the SQLite connection and schema once.
        /// Safe to call multiple times.
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

                // -----------------------------------------------------------------
                // PRAGMAs
                // -----------------------------------------------------------------
                try
                {
                    _ = await _database.ExecuteScalarAsync<long>("PRAGMA foreign_keys = ON;").ConfigureAwait(false);
                    _ = await _database.ExecuteScalarAsync<string>("PRAGMA journal_mode = WAL;").ConfigureAwait(false);
                    _ = await _database.ExecuteScalarAsync<long>("PRAGMA synchronous = NORMAL;").ConfigureAwait(false);
                }
                catch (SQLiteException)
                {
                    // Do not fail startup over PRAGMA support differences.
                }

                // -----------------------------------------------------------------
                // Schema
                // -----------------------------------------------------------------
                await Db.CreateTableAsync<Student>().ConfigureAwait(false);
                await Db.CreateTableAsync<Visit>().ConfigureAwait(false);

                // Visit table is now [Table("Visits")] in Visit.cs
                await Db.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_Visits_StudentDate ON Visits(StudentId, ScheduledDateTime)"
                ).ConfigureAwait(false);

                // Optional future migrations
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
            var version = await db.ExecuteScalarAsync<int>("PRAGMA user_version;").ConfigureAwait(false);

            if (version == 0)
            {
                await db.ExecuteAsync($"PRAGMA user_version = {CurrentSchemaVersion};").ConfigureAwait(false);
                return;
            }

            if (version < 1)
            {
                await db.ExecuteAsync("PRAGMA user_version = 1;").ConfigureAwait(false);
                version = 1;
            }

            // Future migration slots:
            // if (version < 2) { ... }
            // if (version < 3) { ... }
        }

        /// <summary>
        /// Fully-qualified database path (useful for diagnostics).
        /// </summary>
        public string GetDatabasePath() =>
            Path.Combine(FileSystem.AppDataDirectory, DbFileName);

        // -------------------------------------------------------------------------------------------------------------
        // EnsureInitThen helpers
        // -------------------------------------------------------------------------------------------------------------

        internal async Task<T> EnsureInitThen<T>(Func<Task<T>> work, CancellationToken ct = default)
        {
            if (!_initialized)
                await InitializeAsync().ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();
            return await work().ConfigureAwait(false);
        }

        internal async Task EnsureInitThen(Func<Task> work, CancellationToken ct = default)
        {
            if (!_initialized)
                await InitializeAsync().ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();
            await work().ConfigureAwait(false);
        }
    }
}