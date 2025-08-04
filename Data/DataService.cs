using System.Diagnostics;
using MinistryTracker.Models;
using SQLite;

namespace MinistryTracker.Data
{
    /// <summary>
    /// Central data access service for managing the SQLite database.
    /// Handles operations for Student, Household, and Visit models.
    /// Uses asynchronous SQLite API to avoid blocking UI threads.
    /// </summary>
    public class DataService
    {
        private SQLiteAsyncConnection _database;

        /// <summary>
        /// Constructor is intentionally empty because SQLiteAsyncConnection must be initialized asynchronously.
        /// Call InitializeAsync() after constructing this service.
        /// </summary>
        public DataService()
        {
        }

        /// <summary>
        /// Initializes the SQLite database connection and ensures required tables exist.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_database != null)
                return; // Already initialized

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "ministrytracker.db3");
            Debug.WriteLine($"Database path: {dbPath}");

            _database = new SQLiteAsyncConnection(dbPath);

            // Create tables if they do not exist
            await _database.CreateTableAsync<Household>();
            await _database.CreateTableAsync<Student>();
            await _database.CreateTableAsync<Visit>();
        }

        // ---------------- STUDENT CRUD ----------------

        public async Task<int> AddStudentAsync(Student student) =>
            await _database.InsertAsync(student);

        public async Task<List<Student>> GetStudentsAsync() =>
            await _database.Table<Student>()
                           .Where(s => !s.IsDeleted)
                           .ToListAsync();

        public async Task<Student?> GetStudentByIdAsync(int id) =>
            await _database.Table<Student>()
                           .Where(s => s.StudentId == id && !s.IsDeleted)
                           .FirstOrDefaultAsync();

        public async Task<int> UpdateStudentAsync(Student student) =>
            await _database.UpdateAsync(student);

        public async Task<int> SoftDeleteStudentAsync(int id)
        {
            var student = await GetStudentByIdAsync(id);
            if (student != null)
            {
                student.IsDeleted = true;
                return await _database.UpdateAsync(student);
            }
            return 0;
        }

        public async Task<int> PurgeDeletedStudentsAsync()
        {
            var deletedStudents = await _database.Table<Student>()
                                                 .Where(s => s.IsDeleted)
                                                 .ToListAsync();
            int deletedCount = 0;
            foreach (var student in deletedStudents)
            {
                deletedCount += await _database.DeleteAsync(student);
            }
            return deletedCount;
        }

        // ---------------- HOUSEHOLD CRUD ----------------

        public async Task<int> AddHouseholdAsync(Household household) =>
            await _database.InsertAsync(household);

        public async Task<List<Household>> GetHouseholdsAsync() =>
            await _database.Table<Household>().ToListAsync();

        public async Task<Household?> GetHouseholdByIdAsync(int id) =>
            await _database.FindAsync<Household>(id);

        public async Task<int> UpdateHouseholdAsync(Household household) =>
            await _database.UpdateAsync(household);

        public async Task<int> DeleteHouseholdAsync(int id)
        {
            var household = await GetHouseholdByIdAsync(id);
            return household != null ? await _database.DeleteAsync(household) : 0;
        }

        // ---------------- VISIT CRUD ----------------

        /// <summary>
        /// Adds a new visit.
        /// </summary>
        public async Task<int> AddVisitAsync(Visit visit) =>
            await _database.InsertAsync(visit);

        /// <summary>
        /// Gets all visits for a specific student, ordered by scheduled date (most recent first).
        /// </summary>
        public async Task<List<Visit>> GetVisitsForStudentAsync(int studentId) =>
            await _database.Table<Visit>()
                           .Where(v => v.StudentId == studentId)
                           .OrderByDescending(v => v.ScheduledDateTime)
                           .ToListAsync();

        /// <summary>
        /// Updates a visit.
        /// </summary>
        public async Task<int> UpdateVisitAsync(Visit visit) =>
            await _database.UpdateAsync(visit);

        /// <summary>
        /// Deletes a visit by ID.
        /// </summary>
        public async Task<int> DeleteVisitAsync(int id)
        {
            var visit = await _database.FindAsync<Visit>(id);
            return visit != null ? await _database.DeleteAsync(visit) : 0;
        }
    }
}
