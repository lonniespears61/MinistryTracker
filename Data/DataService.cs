using MinistryTracker.Models;
using SQLite;
using System.Diagnostics;

namespace MinistryTracker.Data
{
    /// <summary>
    /// Central data access service for managing the SQLite database.
    /// Handles operations for Student and Household models.
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
        }

        // ---------------- STUDENT CRUD ----------------

        /// <summary>
        /// Adds a new student to the database.
        /// </summary>
        public async Task<int> AddStudentAsync(Student student)
        {
            return await _database.InsertAsync(student);
        }

        /// <summary>
        /// Retrieves a list of all non-deleted students.
        /// </summary>
        public async Task<List<Student>> GetStudentsAsync()
        {
            return await _database.Table<Student>()
                                  .Where(s => !s.IsDeleted)
                                  .ToListAsync();
        }

        /// <summary>
        /// Retrieves a single student by ID, if not marked as deleted.
        /// </summary>
        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _database.Table<Student>()
                                  .Where(s => s.StudentId == id && !s.IsDeleted)
                                  .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Updates an existing student.
        /// </summary>
        public async Task<int> UpdateStudentAsync(Student student)
        {
            return await _database.UpdateAsync(student);
        }

        /// <summary>
        /// Marks a student as deleted instead of physically removing them from the database.
        /// </summary>
        public async Task<int> SoftDeleteStudentAsync(int id)
        {
            var student = await GetStudentByIdAsync(id);
            if (student != null)
            {
                student.IsDeleted = true;
                return await _database.UpdateAsync(student);
            }

            return 0; // Student not found
        }

        /// <summary>
        /// Permanently removes all students who are marked as deleted.
        /// </summary>
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

        /// <summary>
        /// Adds a new household.
        /// </summary>
        public async Task<int> AddHouseholdAsync(Household household)
        {
            return await _database.InsertAsync(household);
        }

        /// <summary>
        /// Retrieves all households.
        /// </summary>
        public async Task<List<Household>> GetHouseholdsAsync()
        {
            return await _database.Table<Household>().ToListAsync();
        }

        /// <summary>
        /// Retrieves a household by ID.
        /// </summary>
        public async Task<Household?> GetHouseholdByIdAsync(int id)
        {
            return await _database.FindAsync<Household>(id);
        }

        /// <summary>
        /// Updates a household record.
        /// </summary>
        public async Task<int> UpdateHouseholdAsync(Household household)
        {
            return await _database.UpdateAsync(household);
        }

        /// <summary>
        /// Deletes a household by ID.
        /// </summary>
        public async Task<int> DeleteHouseholdAsync(int id)
        {
            var household = await GetHouseholdByIdAsync(id);
            return household != null ? await _database.DeleteAsync(household) : 0;
        }
    }
}
