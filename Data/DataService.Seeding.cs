// ---------------------------------------------------------------------------------------------------------------------
// DataService.Seeding.cs
//
// PURPOSE
// - Plausible, deterministic seed data for UI testing
// - Creates Students + Visits using the CURRENT refactored models
//
// DESIGN RULES
// - Student = identity + relationship state
// - Visit = one attempt
// - Household/address is not seeded here yet
// - Keep data realistic enough for list/profile/calendar testing
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

        /// <summary>
        /// Seeds demo data only if there are no non-deleted students.
        /// </summary>
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

                // -----------------------------------------------------------------
                // Seed Students
                // -----------------------------------------------------------------
                var students = BuildSeedStudents(rng);

                foreach (var s in students)
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(s).ConfigureAwait(false);
                }

                // -----------------------------------------------------------------
                // Seed Visits
                // -----------------------------------------------------------------
                var now = DateTime.Now;
                var visits = BuildSeedVisits(rng, students, now);

                foreach (var v in visits)
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(v).ConfigureAwait(false);
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
                "Sofia", "Miguel", "Karen", "Ethan", "Rosa", "Daniel",
                "Hannah", "Noah", "Elena", "Diego", "Grace", "Samuel"
            };

            var lastNames = new[]
            {
                "Gonzalez", "Wilson", "Park", "Hernandez", "Smith", "Davis",
                "Lopez", "Martinez", "Nguyen", "Brown", "Garcia", "Johnson",
                "Anderson", "Taylor", "Thomas", "Moore"
            };

            const int count = 20;
            var results = new List<Student>(capacity: count);

            for (int i = 0; i < count; i++)
            {
                var name = $"{Pick(rng, firstNames)} {Pick(rng, lastNames)}";

                var status = (i % 8 == 0) ? StudentStatus.Paused : StudentStatus.Active;

                var interest = (i % 7 == 0) ? InterestLevel.Promising
                             : (i % 4 == 0) ? InterestLevel.Interested
                             : (i % 3 == 0) ? InterestLevel.ReturnVisit
                             : InterestLevel.Study;

                var initialContactType = (i % 4 == 0) ? InitialContactType.HouseToHouse
                                       : (i % 4 == 1) ? InitialContactType.Cart
                                       : (i % 4 == 2) ? InitialContactType.Phone
                                       : InitialContactType.Letter;

                var defaultMethod = (i % 3 == 0) ? ContactMethod.Text
                                   : (i % 3 == 1) ? ContactMethod.Phone
                                   : ContactMethod.InPerson;

                var phoneNumber = $"270-555-{rng.Next(1000, 9999)}";

                results.Add(new Student
                {
                    Name = name,
                    InitialContactType = initialContactType,
                    FirstContactDate = DateTime.Today.AddDays(-rng.Next(7, 140)),
                    PhoneNumber = phoneNumber,
                    DefaultContactMethod = defaultMethod,
                    InterestLevel = interest,
                    Status = status,
                    IsDoNotCall = false,
                    Notes = BuildPlausibleStudentNotes(rng),
                    IsDeleted = false
                });
            }

            return results;
        }

        private static string BuildPlausibleStudentNotes(Random rng)
        {
            var notes = new[]
            {
                "Friendly and easy to talk with. Good memory aid candidate.",
                "Asked thoughtful questions and seems comfortable texting first.",
                "May prefer shorter follow-up visits due to schedule.",
                "Good conversation about the Kingdom. Worth following up.",
                "Seems more relaxed in informal settings.",
                "Likely best to keep visits simple and consistent."
            };

            return Pick(rng, notes);
        }

        // =====================================================================
        // VISITS
        // =====================================================================

        private static List<Visit> BuildSeedVisits(Random rng, List<Student> students, DateTime now)
        {
            var results = new List<Visit>();

            var studentsWithVisits = students
                .Where((s, idx) => idx % 10 != 0 && s.Status == StudentStatus.Active)
                .ToList();

            foreach (var s in studentsWithVisits)
            {
                var daysAhead = rng.Next(1, 35);
                var hour = rng.Next(9, 19);
                var minute = rng.Next(0, 4) * 15;

                var scheduled = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0)
                    .AddDays(daysAhead)
                    .AddHours(hour)
                    .AddMinutes(minute);

                var stage = (s.InterestLevel == InterestLevel.Study)
                    ? VisitStage.BibleStudy
                    : VisitStage.ReturnVisit;

                var method = s.DefaultContactMethod ?? ContactMethod.InPerson;

                var visit = new Visit
                {
                    StudentId = s.StudentId,
                    Stage = stage,
                    Method = method,
                    ScheduledDateTime = scheduled,
                    Status = VisitStatus.Scheduled,
                    MeetingAddress = BuildPlausibleMeetingPlace(rng, method),
                    Notes = $"Follow-up with {FirstWordOrFallback(s.Name, "student")}."
                };

                results.Add(visit);
            }

            results.Sort((a, b) => a.ScheduledDateTime.CompareTo(b.ScheduledDateTime));
            return results;
        }

        private static string BuildPlausibleMeetingPlace(Random rng, ContactMethod method)
        {
            if (method == ContactMethod.Text)
                return "Text only";

            if (method == ContactMethod.Phone)
                return "Phone";

            if (method == ContactMethod.Email)
                return "Email";

            if (method == ContactMethod.WhatsApp)
                return "WhatsApp";

            var places = new[]
            {
                "Home",
                "The Grind",
                "Work lunch break",
                "Front porch",
                "By the barn",
                "Local park bench"
            };

            return Pick(rng, places);
        }

        private static string FirstWordOrFallback(string? text, string fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : fallback;
        }

        private static T Pick<T>(Random rng, IReadOnlyList<T> items)
            => items[rng.Next(items.Count)];
    }
}