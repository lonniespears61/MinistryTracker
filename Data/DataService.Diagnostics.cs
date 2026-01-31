// ---------------------------------------------------------------------------------------------------------------------
// DataService.Diagnostics.cs (DROP-IN)
//
// PURPOSE:
// - Diagnostics helpers (safe wrappers used by Settings/Diagnostics UI)
// - Keeps destructive or debug-only helpers grouped away from core CRUD
//
// IMPORTANT:
// - This file MUST NOT re-define SeedDemoDataAsync.
//   It only CALLS it (implemented in DataService.Seeding.cs).
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using SQLite;
using MinistryTracker.Models;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        /// <summary>
        /// Runs demo seeding (idempotent).
        /// Safe to expose behind a "Seed Demo Data" button.
        /// </summary>
        public Task RunSeedDemoDataAsync(CancellationToken ct = default)
            => SeedDemoDataAsync(ct);

        /// <summary>
        /// Deletes all rows from Student + Visit tables (hard reset).
        /// Use behind a "Reset DB" button.
        /// </summary>
        public Task ResetDatabaseAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                // Delete child rows first to avoid FK issues.
                await Db.DeleteAllAsync<Visit>().ConfigureAwait(false);
                await Db.DeleteAllAsync<Student>().ConfigureAwait(false);

                // Optional: vacuum can be slow on mobile; keep it off unless needed.
                // await Db.ExecuteAsync("VACUUM;").ConfigureAwait(false);

            }, ct);
        }

        /// <summary>
        /// Quick health check you can display in UI.
        /// </summary>
        public Task<(string DbPath, int StudentCount, int VisitCount)> GetDatabaseHealthAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var students = await Db.Table<Student>().CountAsync().ConfigureAwait(false);
                var visits = await Db.Table<Visit>().CountAsync().ConfigureAwait(false);

                return (GetDatabasePath(), students, visits);
            }, ct);
        }

        /// <summary>
        /// Optional WAL checkpoint. Usually not needed; here for troubleshooting.
        /// </summary>
        public Task ForceWalCheckpointAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await Db.ExecuteAsync("PRAGMA wal_checkpoint(TRUNCATE);").ConfigureAwait(false);
                }
                catch (SQLiteException)
                {
                    // Ignore; not all platforms/configs support WAL the same way.
                }
            }, ct);
        }
    }
}
