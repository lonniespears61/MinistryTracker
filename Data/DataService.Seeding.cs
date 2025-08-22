// ---------------------------------------------------------------------------------------------------------------------
// DataService.Seeding.cs
// Development-only seeding of Visit rows so the UI has something to show.
// SAFE: Always calls InitializeAsync() first and uses the shared Db connection.
// ---------------------------------------------------------------------------------------------------------------------

using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        /// <summary>
        /// Seed Visit rows if (and only if) there are none. No-op if students are absent.
        /// Intended for development/demo; call during app startup AFTER students are seeded.
        /// </summary>
        public async Task SeedVisitsAsync()
        {
            // Defensive: ensure DB and tables exist.
            await InitializeAsync();

            // Avoid duplicate seeds on subsequent runs.
            var existing = await Db.Table<Visit>().CountAsync();
            if (existing > 0) return;

            // We need students to attach visits to.
            var students = await Db.Table<Student>().ToListAsync();
            if (students.Count == 0) return;

            var rnd = new Random(20250815);
            var visits = new List<Visit>();

            DateTime RandomDate(bool future = false)
            {
                var days = future ? rnd.Next(0, 45) : rnd.Next(-60, 30); // mostly past, some future
                var hour = rnd.Next(9, 20);
                var min = new[] { 0, 15, 30, 45 }[rnd.Next(4)];
                return DateTime.Today.AddDays(days).AddHours(hour).AddMinutes(min);
            }

            VisitType PickType()
            {
                // Slightly weighted toward ReturnVisit and BibleStudy.
                var pool = new[]
                {
                    VisitType.ReturnVisit, VisitType.ReturnVisit, VisitType.ReturnVisit,
                    VisitType.BibleStudy, VisitType.BibleStudy,
                    VisitType.InitialCall,
                    VisitType.PhoneCall,
                    VisitType.InformalWitnessing,
                    VisitType.LetterWriting,
                    VisitType.VideoCall,
                    VisitType.CartWitnessing
                };
                return pool[rnd.Next(pool.Length)];
            }

            string[] notesPool =
            {
                "Good conversation at the door.",
                "Left tract and brief invitation.",
                "Reviewed Lesson 1 and set homework.",
                "Answered question about suffering.",
                "Scheduled follow-up for next week.",
                "Prefers evening visits.",
                "Asked for a brochure in their language."
            };

            foreach (var s in students)
            {
                var count = rnd.Next(2, 7);                  // 2–6 visits per student
                var scheduleFuture = rnd.NextDouble() < 0.6; // ~60% have an upcoming one

                for (int i = 0; i < count; i++)
                {
                    var future = (i == count - 1) && scheduleFuture;
                    var when = RandomDate(future);

                    visits.Add(new Visit
                    {
                        StudentId = s.StudentId,
                        ScheduledDateTime = when,
                        VisitType = PickType(),
                        Status = when <= DateTime.Now ? VisitStatus.Completed : VisitStatus.Scheduled,
                        Notes = notesPool[rnd.Next(notesPool.Length)]
                    });
                }
            }

            // A couple of items "today" to make the dashboard feel alive.
            foreach (var s in students.Take(Math.Min(3, students.Count)))
            {
                visits.Add(new Visit
                {
                    StudentId = s.StudentId,
                    ScheduledDateTime = DateTime.Today.AddHours(18),
                    VisitType = VisitType.ReturnVisit,
                    Status = VisitStatus.Scheduled,
                    Notes = "Confirm availability for study."
                });
            }

            await Db.InsertAllAsync(visits);
        }
    }
}
