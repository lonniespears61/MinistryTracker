// ---------------------------------------------------------------------------------------------------------------------
// DataService.cs (base)
// A thin repository-style wrapper around sqlite-net-pcl. This file owns:
//   - Opening the single SQLite connection
//   - One-time database initialization (CreateTableAsync for all entities)
//   - Shared helpers (e.g., exposing the connection via Db)
//
// WHY a single connection?
//   sqlite-net-pcl is lightweight, but opening multiple connections to the same file can invite timing/race hazards,
//   especially if you initialize tables on one connection and query on another. Centralizing here keeps behavior sane.
// ---------------------------------------------------------------------------------------------------------------------

using SQLite;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        // The filename for the SQLite database that lives in the app's sandboxed storage.
        private const string DbFileName = "ministrytracker.db3";

        // The single async connection used everywhere in the app.
        private SQLiteAsyncConnection? _database;

        // Simple guard to ensure InitializeAsync runs once, even if called redundantly.
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _initialized;

        // Centralized accessor so all partials consistently use the same connection.
        private SQLiteAsyncConnection Db =>
            _database ?? throw new InvalidOperationException("Database not initialized. Call InitializeAsync() first.");

        /// <summary>
        /// Create the SQLite connection and the database tables once.
        /// Safe to call multiple times; subsequent calls return immediately.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_initialized) return;

            await _gate.WaitAsync();
            try
            {
                if (_initialized) return;

                // Determine the on-device path for the DB file.
                var path = Path.Combine(FileSystem.AppDataDirectory, DbFileName);

                // Open (or create) the database file.
                _database = new SQLiteAsyncConnection(
                    path,
                    SQLiteOpenFlags.ReadWrite |
                    SQLiteOpenFlags.Create |
                    SQLiteOpenFlags.SharedCache);

                // IMPORTANT: Create all entity tables here.
                // Add any new tables as your domain grows.
                await Db.CreateTableAsync<Models.Student>();
                await Db.CreateTableAsync<Models.Visit>();

                // Optional but recommended: indexes that match your common filters/sorts.
                await Db.ExecuteAsync(
                    "CREATE INDEX IF NOT EXISTS IX_Visit_StudentDate ON Visit(StudentId, ScheduledDateTime)");

                _initialized = true;
            }
            finally
            {
                _gate.Release();
            }
        }

        // --- OPTIONAL HELPER: useful when logging startup issues ---
        /// <summary>
        /// Returns the fully-qualified path to the database file (for logs or support).
        /// </summary>
        public string GetDatabasePath() =>
            Path.Combine(FileSystem.AppDataDirectory, DbFileName);
    }
}
