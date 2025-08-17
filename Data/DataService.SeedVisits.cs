using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite; // sqlite-net-pcl
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Data
{
    public partial class DataService
    {
        // Call this once during startup AFTER seeding students.
        public async Task SeedVisitsAsync()
        {
            // NOTE: This relies on the base DataService having a SQLiteAsyncConnection named "_database".
            // See section #3 if you don't have that.
            var existing = await _database.Table<Visit>().CountAsync();
            if (existing > 0) return;

            var students = await _database.Table<Student>().ToListAsync();
            if (students.Count == 0) return;

            var rnd = new Random(20250815);
            var visits = new List<Visit>();

            DateTime RandomDate(bool future = false)
            {
                var days = future ? rnd.Next(0, 45) : rnd.Next(-60, 30); // mostly past
                var hour = rnd.Next(9, 20);
                var min = new[] { 0, 15, 30, 45 }[rnd.Next(4)];
                return DateTime.Today.AddDays(days).AddHours(hour).AddMinutes(min);
            }

            string[] genericNotes =
            {
                "Good conversation at the door.",
                "Left tract and brief invitation.",
                "Reviewed Lesson 1 and set homework.",
                "Answered question about suffering.",
                "Scheduled follow-up for next week.",
                "Prefers evening visits.",
                "Asked for a brochure in their language."
            };

            // Weighted pool for more realistic distribution
            VisitType PickType()
            {
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

            foreach (var s in students)
            {
                // 2–6 visits per student
                var count = rnd.Next(2, 7);
                var scheduleFuture = rnd.NextDouble() < 0.6; // ~60% have an upcoming one

                for (int i = 0; i < count; i++)
                {
                    var type = PickType();
                    var future = (i == count - 1) && scheduleFuture;
                    var when = RandomDate(future);

                    visits.Add(new Visit
                    {
                        StudentId = s.StudentId,
                        ScheduledDateTime = when,
                        VisitType = type,
                        Status = when <= DateTime.Now ? VisitStatus.Completed : VisitStatus.Scheduled,
                        Notes = genericNotes[rnd.Next(genericNotes.Length)],
                        CancellationReason = null
                    });
                }
            }

            // A few “today” items to make the UI feel alive
            foreach (var s in students.Take(Math.Min(3, students.Count)))
            {
                visits.Add(new Visit
                {
                    StudentId = s.StudentId,
                    ScheduledDateTime = DateTime.Today.AddHours(18),
                    VisitType = VisitType.ReturnVisit,
                    Status = VisitStatus.Scheduled,
                    Notes = "Confirm availability for study.",
                    CancellationReason = null
                });
            }

            // Nicer browsing if you query raw
            visits = visits
                .OrderBy(v => v.StudentId)
                .ThenByDescending(v => v.ScheduledDateTime)
                .ToList();

            await _database.InsertAllAsync(visits);
        }
    }
}
