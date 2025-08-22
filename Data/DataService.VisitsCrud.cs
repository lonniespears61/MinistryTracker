// ---------------------------------------------------------------------------------------------------------------------
// DataService.VisitsCrud.cs
// Simple CRUD for Visit entities. Queries that shape data for the UI live in DataService.Visits.cs.
// ---------------------------------------------------------------------------------------------------------------------

using MinistryTracker.Models;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        /// <summary>
        /// Insert a new visit. On success the Visit.Id will be populated by sqlite-net.
        /// </summary>
        public Task<int> AddVisitAsync(Visit visit)
            => EnsureInitThen(async () => await Db.InsertAsync(visit));

        /// <summary>
        /// Update an existing visit.
        /// </summary>
        public Task<int> UpdateVisitAsync(Visit visit)
            => EnsureInitThen(async () => await Db.UpdateAsync(visit));

        /// <summary>
        /// Fetch a visit by id (or null if not found).
        /// </summary>
        public Task<Visit?> GetVisitByIdAsync(int visitId)
            => EnsureInitThen(async () => await Db.FindAsync<Visit>(visitId));
    }
}
