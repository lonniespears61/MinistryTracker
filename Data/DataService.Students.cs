// DataService.Students.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        /// <summary>Add a new student.</summary>
        public Task<int> AddStudentAsync(Student student, CancellationToken ct = default)
            => EnsureInitThen(() => Db.InsertAsync(student), ct);

        /// <summary>Update an existing student.</summary>
        public Task<int> UpdateStudentAsync(Student student, CancellationToken ct = default)
            => EnsureInitThen(() => Db.UpdateAsync(student), ct);

        /// <summary>Soft-delete (mark IsDeleted) to preserve related data.</summary>
        public Task<int> SoftDeleteStudentAsync(int studentId, CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var s = await Db.FindAsync<Student>(studentId).ConfigureAwait(false);
                if (s is null) return 0;
                s.IsDeleted = true;
                return await Db.UpdateAsync(s).ConfigureAwait(false);
            }, ct);

        /// <summary>List students (optionally include soft-deleted), ordered by Name.</summary>
        public Task<List<Student>> GetStudentsAsync(bool includeDeleted = false, CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var query = Db.Table<Student>();
                if (!includeDeleted)
                    query = query.Where(s => !s.IsDeleted);

                return query.OrderBy(s => s.Name).ToListAsync();
            }, ct);

        /// <summary>Find a student by primary key.</summary>
        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken ct = default)
            => EnsureInitThen(() => Db.FindAsync<Student>(studentId), ct);

        /// <summary>Count active (not soft-deleted) students.</summary>
        public Task<int> GetActiveStudentsCountAsync(CancellationToken ct = default)
            => EnsureInitThen(() =>
                Db.Table<Student>().Where(s => !s.IsDeleted).CountAsync(), ct);

        /// <summary>List students that have GPS coordinates.</summary>
        public Task<List<Student>> GetStudentsWithLocationAsync(
            bool includeDeleted = false,
            CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                var query = Db.Table<Student>()
                    .Where(s => s.StudyLatitude != null && s.StudyLongitude != null);

                if (!includeDeleted)
                    query = query.Where(s => !s.IsDeleted);

                return query.OrderBy(s => s.Name).ToListAsync();
            }, ct);

        /// <summary>
        /// Returns true if it's reasonable to attempt geocoding for this student now.
        /// Prevents repeated API calls.
        /// </summary>
        public bool ShouldAttemptGeocode(Student s, TimeSpan? retryAfter = null)
        {
            if (s is null) return false;

            retryAfter ??= TimeSpan.FromDays(7);

            if (s.GeocodeStatus == GeocodeStatus.None)
                return true;

            if (s.GeocodeStatus == GeocodeStatus.Success)
                return false;

            if (s.LastGeocodeAttemptUtc is null)
                return true;

            var elapsed = DateTime.UtcNow - s.LastGeocodeAttemptUtc.Value;
            return elapsed >= retryAfter.Value;
        }

        /// <summary>
        /// Updates only geocode-related fields (and optional coordinates/address).
        /// </summary>
        public Task<int> UpdateStudentGeocodeAsync(
            int studentId,
            GeocodeStatus status,
            DateTime? lastAttemptUtc = null,
            double? latitude = null,
            double? longitude = null,
            string? studyAddress = null,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var s = await Db.FindAsync<Student>(studentId).ConfigureAwait(false);
                if (s is null) return 0;

                s.GeocodeStatus = status;
                s.LastGeocodeAttemptUtc = lastAttemptUtc ?? DateTime.UtcNow;

                if (latitude.HasValue) s.StudyLatitude = latitude.Value;
                if (longitude.HasValue) s.StudyLongitude = longitude.Value;
                if (studyAddress is not null) s.StudyAddress = studyAddress;

                return await Db.UpdateAsync(s).ConfigureAwait(false);
            }, ct);
    }
}
