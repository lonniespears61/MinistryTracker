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
// CHANGE NOTES (2026-XX-XX)
// - Added schema version tracking (PRAGMA user_version)
// - Added helpers to compare DB version vs app version
// - Structured migration pipeline for future updates
// - No migrations implemented yet (this is baseline version 1)
//
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using MinistryTracker.Models;
using MinistryTracker.Data.Repositories;
using MinistryTracker.Data.Security;

namespace MinistryTracker.Data
{
    public partial class DataService : IStudentRepository, IVisitRepository
    {
        private SQLiteAsyncConnection? _database;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly string _databasePath;
        private readonly IDataProtectionService _dataProtection;
        private bool _initialized;

        public DataService(string databasePath, IDataProtectionService dataProtection)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
                throw new ArgumentException("Database path is required.", nameof(databasePath));

            _databasePath = databasePath;
            _dataProtection = dataProtection ?? throw new ArgumentNullException(nameof(dataProtection));
        }

        // -----------------------------------------------------------------------------------------------------------------
        // SCHEMA VERSION (SOURCE OF TRUTH FOR THIS BUILD)
        // -----------------------------------------------------------------------------------------------------------------
        // WHY:
        // - This represents what the app expects the DB structure to be
        // - We compare this against PRAGMA user_version to detect drift
        // - For now, this is our baseline (fresh DB = version 1)
        private const int CurrentSchemaVersion = 2;

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

                await _dataProtection.InitializeAsync().ConfigureAwait(false);

                _database = new SQLiteAsyncConnection(
                    _databasePath,
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
                // SCHEMA CREATION
                // -----------------------------------------------------------------
                await Db.CreateTableAsync<Student>().ConfigureAwait(false);
                await Db.CreateTableAsync<Visit>().ConfigureAwait(false);
                await Db.CreateTableAsync<DataProtectionMetadata>().ConfigureAwait(false);

                await Db.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_Visits_StudentDate ON Visits(StudentId, ScheduledDateTime)"
                ).ConfigureAwait(false);

                // -----------------------------------------------------------------
                // SCHEMA VERSION + MIGRATIONS
                // -----------------------------------------------------------------
                // WHY:
                // - Ensures DB structure matches what this build expects
                // - Right now this just sets baseline version (no migrations yet)
                await ApplyMigrationsAsync(_database).ConfigureAwait(false);
                await VerifyDataProtectionKeyAsync(_database).ConfigureAwait(false);

                _initialized = true;
            }
            finally
            {
                _gate.Release();
            }
        }

        // -----------------------------------------------------------------------------------------------------------------
        // SCHEMA VERSION HELPERS
        // -----------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Returns the schema version stored in SQLite.
        /// </summary>
        public Task<int> GetDatabaseSchemaVersionAsync()
        {
            return EnsureInitThen(() =>
                Db.ExecuteScalarAsync<int>("PRAGMA user_version;"));
        }

        /// <summary>
        /// Returns the schema version expected by this app build.
        /// </summary>
        public int GetAppSchemaVersion() => CurrentSchemaVersion;

        /// <summary>
        /// True when DB is behind current app schema.
        /// </summary>
        public async Task<bool> IsMigrationRequiredAsync()
        {
            var dbVersion = await GetDatabaseSchemaVersionAsync().ConfigureAwait(false);
            return dbVersion < CurrentSchemaVersion;
        }

        // -----------------------------------------------------------------------------------------------------------------
        // MIGRATION PIPELINE
        // -----------------------------------------------------------------------------------------------------------------
        // WHY:
        // - Keeps DB evolution controlled as app grows
        // - Prevents silent schema drift between versions
        // - Allows future upgrades without forcing resets

        private async Task ApplyMigrationsAsync(SQLiteAsyncConnection db)
        {
            var version = await db.ExecuteScalarAsync<int>("PRAGMA user_version;").ConfigureAwait(false);

            // -----------------------------------------------------------------
            // BASELINE (VERSION 1)
            // -----------------------------------------------------------------
            // WHY:
            // - Fresh DB starts at version 0
            // - We explicitly set version so future migrations have a reference point
            if (version == 0)
            {
                await MigrateLegacyPlaintextAsync(db).ConfigureAwait(false);
                await db.ExecuteAsync($"PRAGMA user_version = {CurrentSchemaVersion};").ConfigureAwait(false);
                return;
            }

            if (version < 2)
            {
                await MigrateLegacyPlaintextAsync(db).ConfigureAwait(false);
                await db.ExecuteAsync("PRAGMA user_version = 2;").ConfigureAwait(false);
                version = 2;
            }

            // -----------------------------------------------------------------
            // FUTURE MIGRATIONS (DO NOT REMOVE - expand when needed)
            // -----------------------------------------------------------------

            // if (version < 2)
            // {
            //     // Example:
            //     // await db.ExecuteAsync("ALTER TABLE Students ADD COLUMN Example TEXT;");
            //
            //     await db.ExecuteAsync("PRAGMA user_version = 2;").ConfigureAwait(false);
            //     version = 2;
            // }

            // if (version < 3)
            // {
            //     await db.ExecuteAsync("PRAGMA user_version = 3;").ConfigureAwait(false);
            //     version = 3;
            // }
        }

        private sealed class DataProtectionMetadata
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string ProtectedVerificationValue { get; set; } = string.Empty;
        }

        /// <summary>
        /// Fully-qualified database path (useful for diagnostics).
        /// </summary>
        public string GetDatabasePath() =>
            _databasePath;

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
