// ---------------------------------------------------------------------------------------------------------------------
// DataService.VisitsCrud.cs (DROP-IN - corrected)
//
// ✅ Adds CancellationToken to ALL methods (standard + consistent)
// ✅ Uses EnsureInitThen(() => Db.XxxAsync(...), ct) (clean, idiomatic for sqlite-net-pcl)
// ✅ Validates inputs early (fail fast, easier debugging)
// ✅ Keeps signatures compatible with your existing callers (returns int, Visit?)
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using MinistryTracker.Models;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        // -------------------------------------------------------------------------------------------------------------
        // Visits – CRUD
        // -------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Insert a new Visit.
        /// Returns number of rows inserted (sqlite-net usually returns 1).
        /// After insert, sqlite-net sets visit.Id automatically (because it's [AutoIncrement]).
        /// </summary>
        public Task<int> AddVisitAsync(Visit visit, CancellationToken ct = default)
        {
            if (visit is null)
                throw new ArgumentNullException(nameof(visit));

            if (visit.StudentId <= 0)
                throw new InvalidOperationException("Visit.StudentId must be set before inserting a visit.");

            // No need for async/await wrapper — Db.InsertAsync already returns Task<int>.
            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.InsertAsync(visit);
            }, ct);
        }

        /// <summary>
        /// Update an existing Visit (by primary key).
        /// Returns rows affected (0 means not found).
        /// </summary>
        public Task<int> UpdateVisitAsync(Visit visit, CancellationToken ct = default)
        {
            if (visit is null)
                throw new ArgumentNullException(nameof(visit));

            if (visit.Id <= 0)
                throw new InvalidOperationException("Visit.Id must be set before updating a visit.");

            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.UpdateAsync(visit);
            }, ct);
        }

        /// <summary>
        /// Get a visit by primary key.
        /// Returns null if not found.
        /// </summary>
        public Task<Visit?> GetVisitByIdAsync(int visitId, CancellationToken ct = default)
        {
            if (visitId <= 0)
                return Task.FromResult<Visit?>(null);

            // Db.FindAsync is fine here because it queries by PK.
            // NOTE: This does not filter by Status; caller decides what "visible" means.
            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.FindAsync<Visit>(visitId);
            }, ct);
        }

        /// <summary>
        /// Delete a visit row (hard delete).
        /// NOTE: If you later decide you want soft-delete for visits too,
        /// add an IsDeleted flag to Visit and convert this method.
        /// </summary>
        public Task<int> DeleteVisitAsync(int visitId, CancellationToken ct = default)
        {
            if (visitId <= 0)
                return Task.FromResult(0);

            return EnsureInitThen(() =>
            {
                ct.ThrowIfCancellationRequested();
                return Db.DeleteAsync<Visit>(visitId);
            }, ct);
        }
    }
}
