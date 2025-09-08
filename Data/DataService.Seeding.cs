// ---------------------------------------------------------------------------------------------------------------------
// DataService.Seeding.cs
// Development-only seeding so the UI has something to show.
// - Unified init via EnsureInitThen(...)
// - Supports CancellationToken
// - "force" will clear existing rows before inserting demo data
// ---------------------------------------------------------------------------------------------------------------------

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
        /// <summary>
        /// Seed a small, deterministic set of Students + Visits.
        /// If <paramref name="force"/> is false, does nothing when Students already exist.
        /// If <paramref name="force"/> is true, clears Students/Visits and re-inserts demo data.
        /// </summary>
        public Task SeedDevDataAsync(bool force = false, CancellationToken ct = default)
            => EnsureInitThen(async () =>
            {
                // If forcing, wipe tables first (best-effort)
                if (force)
                {
                    try { await Db.DeleteAllAsync<Visit>().ConfigureAwait(false); } catch { }
                    try { await Db.DeleteAllAsync<Student>().ConfigureAwait(false); } catch { }
                }

                // Skip if we already have students and not forcing
                var existingStudents = await Db.Table<Student>().CountAsync().ConfigureAwait(false);
                if (existingStudents > 0 && !force) return;

                // ---- Seed Students (minimal fields; extend as your model grows) ----
                var s1 = new Student { Name = "Jane Doe", Status = StudentStatus.Active };
                var s2 = new Student { Name = "John Smith", Status = StudentStatus.Active };
                var s3 = new Student { Name = "Avery Lee", Status = StudentStatus.Active };
                var s4 = new Student { Name = "Priya K.", Status = StudentStatus.Active };

                await Db.InsertAllAsync(new[] { s1, s2, s3, s4 }).ConfigureAwait(false);

                // ---- Seed Visits (a few per student; some past, some upcoming) ----
                var rnd = new Random(20250815);
                DateTime RandomDate(bool future = false)
                {
                    var days = future ? rnd.Next(0, 30) : rnd.Next(-30, 5);
                    var hour = rnd.Next(9, 20);
                    var min = new[] { 0, 15, 30, 45 }[rnd.Next(4)];
                    return DateTime.Today.AddDays(days).AddHours(hour).AddMinutes(min);
                }

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

                var students = new[] { s1, s2, s3, s4 };
                var visits = new List<Visit>();

                foreach (var s in students)
                {
                    ct.ThrowIfCancellationRequested();

                    var count = rnd.Next(2, 5);                     // 2–4 visits per student
                    var scheduleFuture = rnd.NextDouble() < 0.6;    // ~60% have an upcoming

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
                foreach (var s in students.Take(Math.Min(3, students.Length)))
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

                ct.ThrowIfCancellationRequested();
                await Db.InsertAllAsync(visits).ConfigureAwait(false);
            }, ct);
    }
}
