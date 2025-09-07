// ---------------------------------------------------------------------------------------------------------------------
// DataService.Students.cs
// Student-centric CRUD and queries. Keeping this in a partial keeps DataService tidy and discoverable.
// Uses the single shared SQLiteAsyncConnection exposed via the `Db` property in DataService base.
// ---------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinistryTracker.Models;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        /// <summary>
        /// Insert a new student. Returns the number of rows inserted (1 on success).
        /// NOTE: After InsertAsync, sqlite-net sets the auto-increment PK back on the entity (StudentId).
        /// </summary>
        public async Task<int> AddStudentAsync(Student student)
        {
            // Defensive: ensure DB is initialized in case caller forgot.
            // (InitializeAsync is idempotent; cheap if already initialized.)
            await InitializeAsync();
            return await Db.InsertAsync(student);
        }

        /// <summary>
        /// Update an existing student. Returns rows affected (1 on success).
        /// </summary>
        public async Task<int> UpdateStudentAsync(Student student)
        {
            await InitializeAsync();
            return await Db.UpdateAsync(student);
        }

        /// <summary>
        /// Soft-delete a student (if you support it): mark IsDeleted = true.
        /// Keeps historical visits intact while hiding from day-to-day UI.
        /// </summary>
        public async Task<int> SoftDeleteStudentAsync(int studentId)
        {
            await InitializeAsync();
            var s = await Db.FindAsync<Student>(studentId);
            if (s is null) return 0;
            s.IsDeleted = true;
            return await Db.UpdateAsync(s);
        }

        /// <summary>
        /// Get all students for UI lists. By default hides deleted; pass includeDeleted=true to fetch all.
        /// </summary>
        public async Task<List<Student>> GetStudentsAsync(bool includeDeleted = false)
        {
            await InitializeAsync();

            var query = Db.Table<Student>();
            if (!includeDeleted)
                query = query.Where(s => !s.IsDeleted);

            // Order by name for UX niceness. Adjust to your preference.
            return await query.OrderBy(s => s.Name).ToListAsync();
        }

        /// <summary>
        /// Find a student by primary key or return null if not found.
        /// </summary>
        public async Task<Student?> GetStudentByIdAsync(int studentId)
        {
            await InitializeAsync();
            return await Db.FindAsync<Student>(studentId);
        }

        /// <summary>
        /// Count active (non-deleted) students.
        /// </summary>
        public async Task<int> GetActiveStudentsCountAsync()
        {
            await InitializeAsync();
            return await Db.Table<Student>().Where(s => !s.IsDeleted).CountAsync();
        }
    }
}
