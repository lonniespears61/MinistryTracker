// DataService.Students.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MinistryTracker.Models;

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
            => EnsureInitThen(() => Db.Table<Student>().Where(s => !s.IsDeleted).CountAsync(), ct);
    }
}
