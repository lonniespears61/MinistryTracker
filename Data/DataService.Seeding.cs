// ---------------------------------------------------------------------------------------------------------------------
// DataService.Seeding.cs
//
// PURPOSE
// - Plausible, deterministic seed data for UI testing
// - Creates Students + Visits using the current models
//
// DESIGN RULES
// - Student = identity + relationship state + reachable anchor
// - Visit = one scheduled attempt
// - No dead concepts (VisitStage / VisitType / DoNotCall)
// - Keep data realistic enough for dashboard, list, profile, and map testing
//
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
        private const int SeedRandom = 42;

        public Task SeedDemoDataAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                var existingCount = await Db.Table<Student>()
                    .Where(s => !s.IsDeleted)
                    .CountAsync()
                    .ConfigureAwait(false);

                if (existingCount > 0)
                    return;

                var rng = new Random(SeedRandom);

                var students = BuildSeedStudents(rng);

                foreach (var student in students)
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(student).ConfigureAwait(false);
                }

                var now = DateTime.Now;
                var visits = BuildSeedVisits(rng, students, now);

                foreach (var visit in visits)
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(visit).ConfigureAwait(false);
                }

            }, ct);
        }

        // =====================================================================
        // STUDENTS
        // =====================================================================

        private static List<Student> BuildSeedStudents(Random rng)
        {
            var firstNames = new[]
            {
                "Maria", "James", "Linda", "Carlos", "Ashley", "Robert",
                "Sofia", "Miguel", "Karen", "Ethan", "Rosa", "Daniel"
            };

            var lastNames = new[]
            {
                "Gonzalez", "Wilson", "Park", "Hernandez", "Smith", "Davis",
                "Lopez", "Martinez", "Nguyen", "Brown", "Garcia"
            };

            const int count = 20;
            var results = new List<Student>(count);

            for (int i = 0; i < count; i++)
            {
                var name = $"{Pick(rng, firstNames)} {Pick(rng, lastNames)}";

                var status =
                    (i % 11 == 0) ? StudentStatus.Completed :
                    (i % 9 == 0) ? StudentStatus.Discontinued :
                    (i % 6 == 0) ? StudentStatus.Paused :
                    StudentStatus.Active;

                // ✅ FIXED INTEREST LEVEL
                var interest =
                    (i % 7 == 0) ? InterestLevel.Study :
                    (i % 4 == 0) ? InterestLevel.ReturnVisit :
                    (i % 3 == 0) ? InterestLevel.Interested :
                    InterestLevel.Promising;

                var initialContactType =
                    (i % 4 == 0) ? InitialContactType.HouseToHouse :
                    (i % 4 == 1) ? InitialContactType.Cart :
                    (i % 4 == 2) ? InitialContactType.Phone :
                    InitialContactType.Letter;

                results.Add(new Student
                {
                    Name = name,
                    InitialContactType = initialContactType,
                    FirstContactDate = DateTime.Today.AddDays(-rng.Next(15, 180)),
                    PhoneNumber = $"270-555-{rng.Next(1000, 9999)}",
                    PreferredContactMethod = ContactMethod.InPerson,
                    InterestLevel = interest,
                    Status = status,
                    Notes = "Good conversation. Worth following up.",
                    IsDeleted = false
                });
            }

            return results;
        }

        // =====================================================================
        // VISITS
        // =====================================================================

        private static List<Visit> BuildSeedVisits(Random rng, List<Student> students, DateTime now)
        {
            var results = new List<Visit>();

            var studentsWithVisits = students
                .Where(s => s.Status == StudentStatus.Active || s.Status == StudentStatus.Paused)
                .Take(15)
                .ToList();

            foreach (var student in studentsWithVisits)
            {
                var scheduled = now.AddDays(rng.Next(-30, 20))
                    .AddHours(rng.Next(9, 18));

                var isPast = scheduled < now;

                var status = isPast
                    ? VisitStatus.Successful
                    : VisitStatus.Scheduled;

                results.Add(new Visit
                {
                    StudentId = student.StudentId,
                    Method = ContactMethod.InPerson,
                    ScheduledDateTime = scheduled,
                    Status = status,
                    Notes = "Follow up discussion"
                });
            }

            return results.OrderBy(v => v.ScheduledDateTime).ToList();
        }

        private static T Pick<T>(Random rng, IReadOnlyList<T> items)
            => items[rng.Next(items.Count)];
    }
}