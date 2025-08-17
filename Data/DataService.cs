using System.Diagnostics;
using MinistryTracker.Models;
using SQLite;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        private readonly SemaphoreSlim _initGate = new(1, 1);
        private SQLiteAsyncConnection? _database;

        public async Task InitializeAsync()
        {
            if (_database is not null) return;

            await _initGate.WaitAsync();
            try
            {
                if (_database is not null) return;

                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "ministrytracker.db3");
                Debug.WriteLine($"Database path: {dbPath}");

                _database = new SQLiteAsyncConnection(
                    dbPath,
                    SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache
                );

                await _database.CreateTableAsync<Household>();
                await _database.CreateTableAsync<Student>();
                await _database.CreateTableAsync<Visit>();
            }
            finally { _initGate.Release(); }
        }

        private SQLiteAsyncConnection Db =>
            _database ?? throw new InvalidOperationException("Database not initialized. Call InitializeAsync() once at startup.");

        // ----- STUDENT CRUD -----
        public Task<int> AddStudentAsync(Student student) => Db.InsertAsync(student);
        public Task<List<Student>> GetStudentsAsync() =>
            Db.Table<Student>().Where(s => !s.IsDeleted).ToListAsync();

        public Task<Student?> GetStudentByIdAsync(int id) =>
            Db.Table<Student>().Where(s => s.StudentId == id && !s.IsDeleted).FirstOrDefaultAsync();

        public Task<int> UpdateStudentAsync(Student student) => Db.UpdateAsync(student);

        public async Task<int> SoftDeleteStudentAsync(int id)
        {
            var student = await GetStudentByIdAsync(id);
            if (student is null) return 0;
            student.IsDeleted = true;
            return await Db.UpdateAsync(student);
        }

        public async Task<int> PurgeDeletedStudentsAsync()
        {
            var deleted = await Db.Table<Student>().Where(s => s.IsDeleted).ToListAsync();
            var count = 0;
            foreach (var s in deleted) count += await Db.DeleteAsync(s);
            return count;
        }

        // ----- HOUSEHOLD CRUD -----
        public Task<int> AddHouseholdAsync(Household household) => Db.InsertAsync(household);
        public Task<List<Household>> GetHouseholdsAsync() => Db.Table<Household>().ToListAsync();
        public Task<Household?> GetHouseholdByIdAsync(int id) => Db.FindAsync<Household>(id);
        public Task<int> UpdateHouseholdAsync(Household household) => Db.UpdateAsync(household);
        public async Task<int> DeleteHouseholdAsync(int id)
        {
            var h = await GetHouseholdByIdAsync(id);
            return h != null ? await Db.DeleteAsync(h) : 0;
        }

        // ----- VISIT CRUD -----
        public Task<int> AddVisitAsync(Visit visit) => Db.InsertAsync(visit);
        public Task<List<Visit>> GetVisitsForStudentAsync(int studentId) =>
            Db.Table<Visit>().Where(v => v.StudentId == studentId)
              .OrderByDescending(v => v.ScheduledDateTime)
              .ToListAsync();
        public Task<int> UpdateVisitAsync(Visit visit) => Db.UpdateAsync(visit);
        public async Task<int> DeleteVisitAsync(int id)
        {
            var v = await Db.FindAsync<Visit>(id);
            return v != null ? await Db.DeleteAsync(v) : 0;
        }
    }
}
