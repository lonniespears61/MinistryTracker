// ---------------------------------------------------------------------------------------------------------------------
// DataService.Seeding.cs
// Development-only seeded data for Students + Visits.
// Deterministic, idempotent, and safe: always calls InitializeAsync() and checks counts.
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        /// <summary>
        /// Public entrypoint for dev/demo seeding.
        /// - No-ops if data exists (unless force=true).
        /// - Seeds a small Student set, then Visit rows attached to those students.
        /// </summary>
        public async Task SeedDevDataAsync(bool force = false)
        {
            await InitializeAsync().ConfigureAwait(false);

            if (force)
            {
                try { await Db.DeleteAllAsync<Visit>().ConfigureAwait(false); } catch { }
                try { await Db.DeleteAllAsync<Student>().ConfigureAwait(false); } catch { }
            }

            var studentCount = 0;
            var visitCount = 0;
            try { studentCount = await Db.Table<Student>().CountAsync().ConfigureAwait(false); } catch { }
            try { visitCount = await Db.Table<Visit>().CountAsync().ConfigureAwait(false); } catch { }

            if (studentCount == 0)
                await SeedStudentsAsync().ConfigureAwait(false);

            if (visitCount == 0)
                await SeedVisitsAgainstExistingStudentsAsync().ConfigureAwait(false);
        }

        /// <summary>Deterministic sample students. Idempotent.</summary>
        private async Task SeedStudentsAsync()
        {
            var existing = await Db.Table<Student>().CountAsync().ConfigureAwait(false);
            if (existing > 0) return;

            await Db.InsertAllAsync(new[]
            {
                new Student { Name = "Jane Doe",   Status = StudentStatus.Active },
                new Student { Name = "John Smith", Status = StudentStatus.Active },
                new Student { Name = "Avery Brown",Status = StudentStatus.Active },
                new Student { Name = "Chris Lee",  Status = StudentStatus.Active }
            }).ConfigureAwait(false);
        }

        /// <summary>Seeds visits for all current students, weighted by type and time distribution. Idempotent.</summary>
        private async Task SeedVisitsAgainstExistingStudentsAsync()
        {
            var existing = await Db.Table<Visit>().CountAsync().ConfigureAwait(false);
            if (existing > 0) return;

            var students = await Db.Table<Student>().ToListAsync().ConfigureAwait(false);
            if (students.Count == 0) return;

            var rnd = new Random(20250815); // deterministic seed
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

            await Db.InsertAllAsync(visits).ConfigureAwait(false);
        }
    }
}
