// ---------------------------------------------------------------------------------------------------------------------
// DataService.Students.cs
//
// PURPOSE
// - Student-focused CRUD operations and student-level query helpers.
// - Keeps Student access aligned with the current Student model.
//
// DESIGN RULES
// - Student stores identity, relationship state, preferred contact path,
//   and the primary reachable physical location.
// - Visit-specific meeting places belong on Visit, not Student.
// - This file provides data access only; UI validation belongs elsewhere.
//
// ---------------------------------------------------------------------------------------------------------------------

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
        /// <summary>
        /// Add a new student.
        /// </summary>
        public Task<int> AddStudentAsync(Student student, CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                ProtectForWrite(student);
                return Db.InsertAsync(student);
            }, ct);

        /// <summary>
        /// Update an existing student.
        /// </summary>
        public Task<int> UpdateStudentAsync(Student student, CancellationToken ct = default)
            => EnsureInitThen(() =>
            {
                ProtectForWrite(student);
                return Db.UpdateAsync(student);
            }, ct);

        /// <summary>
        /// Soft-delete a student by marking IsDeleted = true.
        /// This preserves visit history and related records.
        /// </summary>
        public Task<int> SoftDeleteStudentAsync(int studentId, CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var student = await Db.FindAsync<Student>(studentId).ConfigureAwait(false);
                if (student is null)
                    return 0;

                student.IsDeleted = true;
                return await Db.UpdateAsync(student).ConfigureAwait(false);
            }, ct);

        /// <summary>
        /// Find a student by primary key.
        /// Returns null if not found.
        /// </summary>
        public Task<Student?> GetStudentByIdAsync(int studentId, CancellationToken ct = default)
     => EnsureInitThen<Student?>(async () =>
     {
         var student = await Db.FindAsync<Student>(studentId).ConfigureAwait(false);
         if (student is not null)
             UnprotectAfterRead(student);
         return student;
     }, ct);

        /// <summary>
        /// Get all students, optionally including soft-deleted records.
        /// Results are ordered by Name.
        /// </summary>
        public Task<List<Student>> GetStudentsAsync(bool includeDeleted = false, CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var query = Db.Table<Student>();

                if (!includeDeleted)
                    query = query.Where(s => !s.IsDeleted);

                var students = await query.ToListAsync().ConfigureAwait(false);
                return UnprotectStudents(students)
                    .OrderBy(s => s.Name)
                    .ToList();
            }, ct);

        /// <summary>
        /// Get students that are still in normal working scope.
        /// Completed and Discontinued are excluded.
        /// Paused stays in scope because it can resurface later.
        /// </summary>
        public Task<List<Student>> GetWorkingScopeStudentsAsync(CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var students = await Db.Table<Student>()
                  .Where(s =>
                      !s.IsDeleted &&
                      s.Status != StudentStatus.Completed &&
                      s.Status != StudentStatus.Discontinued)
                  .ToListAsync()
                  .ConfigureAwait(false);

                return UnprotectStudents(students)
                    .OrderBy(s => s.Name)
                    .ToList();
            }, ct);

        /// <summary>
        /// Count students still in normal working scope.
        /// </summary>
        public Task<int> GetActiveStudentsCountAsync(CancellationToken ct = default)
            => EnsureInitThen(() =>
                Db.Table<Student>()
                  .Where(s =>
                      !s.IsDeleted &&
                      s.Status != StudentStatus.Completed &&
                      s.Status != StudentStatus.Discontinued)
                  .CountAsync(), ct);

        /// <summary>
        /// Returns students that can be shown on the map.
        /// Map scope is limited to in-person-capable students with a usable primary location.
        /// </summary>
        public Task<List<Student>> GetMappableStudentsAsync(CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var students = await Db.Table<Student>()
                  .Where(s =>
                      !s.IsDeleted &&
                      s.Status != StudentStatus.Completed &&
                      s.Status != StudentStatus.Discontinued &&
                      (
                          s.PreferredContactMethod == null ||
                          s.PreferredContactMethod == ContactMethod.InPerson
                      ))
                  .ToListAsync()
                  .ConfigureAwait(false);

                return UnprotectStudents(students)
                    .Where(s =>
                        s.PrimaryLatitude is not null &&
                        s.PrimaryLongitude is not null &&
                        !string.IsNullOrWhiteSpace(s.PrimaryAddress))
                    .OrderBy(s => s.Name)
                    .ToList();
            }, ct);

        /// <summary>
        /// Returns students sharing the same home address.
        /// Only addresses explicitly marked as home are eligible.
        /// </summary>
        public Task<List<Student>> GetStudentsByHomeAddressAsync(string primaryAddress, CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var students = await Db.Table<Student>()
                  .Where(s =>
                      !s.IsDeleted &&
                      s.IsHomeAddress)
                  .ToListAsync()
                  .ConfigureAwait(false);

                return UnprotectStudents(students)
                    .Where(s => string.Equals(
                        s.PrimaryAddress,
                        primaryAddress,
                        StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s.Name)
                    .ToList();
            }, ct);

        /// <summary>
        /// Updates only the student's primary location fields.
        /// Keeps location changes isolated from broader student edits.
        /// </summary>
        public Task<int> UpdateStudentPrimaryLocationAsync(
            int studentId,
            string? primaryAddress,
            bool isHomeAddress,
            LocationContext locationContext,
            double? primaryLatitude,
            double? primaryLongitude,
            GeocodeStatus geocodeStatus,
            CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                var student = await Db.FindAsync<Student>(studentId).ConfigureAwait(false);
                if (student is null)
                    return 0;

                UnprotectAfterRead(student);
                student.PrimaryAddress = primaryAddress;
                student.IsHomeAddress = isHomeAddress;
                student.LocationContext = locationContext;
                student.PrimaryLatitude = primaryLatitude;
                student.PrimaryLongitude = primaryLongitude;
                student.PrimaryGeocodeStatus = geocodeStatus;
                ProtectForWrite(student);

                return await Db.UpdateAsync(student).ConfigureAwait(false);
            }, ct);
    }
}
