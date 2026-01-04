// ---------------------------------------------------------------------------------------------------------------------
// DataService.Seeding.cs  (DROP-IN - richer, plausible, non-geocoded seed data)
//
// GOAL:
// - Provide "realistic enough" Students + Visits for UI testing:
//     - list/search/filter
//     - profile/edit
//     - calendar grid + agenda
//     - near-me map (GPS clustering)
//
// KEY RULES:
// - Addresses are PLAUSIBLE strings (not geocoded).
// - GPS coordinates are REALISTIC and clustered around Tompkinsville, KY within 75 miles.
// - One FUTURE visit per student is the norm (we seed at most one).
// - Uses EnsureInitThen + CancellationToken.
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
        // Center: Tompkinsville, KY (Monroe County seat)
        // Wikipedia lists approx: (36.699508, -85.692005)
        private const double CenterLat = 36.699508;
        private const double CenterLon = -85.692005;

        private const double MaxMilesFromCenter = 75.0;

        // Keep dev seeds deterministic so "it worked yesterday" is repeatable.
        // Change this number when you want a different dataset.
        private const int SeedRandom = 42;

        /// <summary>
        /// Seeds demo data (safe default).
        /// Does nothing if real data already exists.
        /// </summary>
        public Task SeedDemoDataAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                // Guard: if any non-deleted students exist, assume this is a real DB.
                var existingCount = await Db.Table<Student>()
                    .Where(s => !s.IsDeleted)
                    .CountAsync()
                    .ConfigureAwait(false);

                if (existingCount > 0)
                    return;

                var rng = new Random(SeedRandom);

                // ---------------------------------------------------------------------
                // Seed Students (richer data; fill fields needed for testing)
                // ---------------------------------------------------------------------
                // NOTE: These are plausible names + plausible local-area town labels,
                // but the addresses are NOT geocoded (by design).
                var students = BuildSeedStudents(rng);

                // Insert students (sqlite-net will populate StudentId on insert)
                foreach (var s in students)
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(s).ConfigureAwait(false);
                }

                // ---------------------------------------------------------------------
                // Seed Visits (one FUTURE scheduled visit per student, not multiple)
                // ---------------------------------------------------------------------
                var now = DateTime.Now;

                var visits = BuildSeedVisits(rng, students, now);

                foreach (var v in visits)
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(v).ConfigureAwait(false);
                }

            }, ct);
        }

        // -------------------------------------------------------------------------------------------------------------
        // STUDENTS
        // -------------------------------------------------------------------------------------------------------------

        private static List<Student> BuildSeedStudents(Random rng)
        {
            // Local-flavor community names (Monroe County area examples)
            // (These are used as "town" labels only; not geocoded.) 
            var towns = new[]
            {
                "Tompkinsville", "Gamaliel", "Fountain Run", "Mud Lick", "Sulphur Lick",
                "Flippin", "Meshack", "Hestand"
            };

            var firstNames = new[] { "Maria", "James", "Linda", "Carlos", "Ashley", "Robert", "Sofia", "Miguel", "Karen", "Ethan", "Rosa", "Daniel" };
            var lastNames = new[] { "Gonzalez", "Wilson", "Park", "Hernandez", "Smith", "Davis", "Lopez", "Martinez", "Nguyen", "Brown", "Garcia", "Johnson" };

            // Generate ~20 students (enough to test scrolling/search)
            var count = 20;

            var results = new List<Student>(capacity: count);

            for (int i = 0; i < count; i++)
            {
                var name = $"{Pick(rng, firstNames)} {Pick(rng, lastNames)}";

                var (lat, lon) = RandomPointWithinMiles(rng, CenterLat, CenterLon, MaxMilesFromCenter);

                var town = Pick(rng, towns);
                var address = BuildPlausibleAddress(rng, town);

                // Mix English/Spanish for early testing (even before localization UI)
                var preferredLanguage = (i % 5 == 0) ? "Spanish" : "English";

                var status = (i % 8 == 0) ? StudentStatus.Paused : StudentStatus.Active;

                var interest = (i % 7 == 0) ? InterestLevel.Potential
                             : (i % 4 == 0) ? InterestLevel.Interested
                             : InterestLevel.Study;

                var callType = (i % 4 == 0) ? InitialCallType.HouseToHouse
                             : (i % 4 == 1) ? InitialCallType.Cart
                             : (i % 4 == 2) ? InitialCallType.Phone
                             : InitialCallType.Letter;

                // Optional fields: fill enough variety to test UI formatting
                    Gender? gender =
                          (i % 3 == 0) ? Gender.Female
                         : (i % 3 == 1) ? Gender.Male
                         : (Gender?)null;
                results.Add(new Student
                {
                    Name = name,
                    CallType = callType,
                    FirstContactDate = DateTime.Today.AddDays(-rng.Next(7, 140)),
                    StudyAddress = address,
                    StudyLatitude = lat,
                    StudyLongitude = lon,
                    PreferredLanguage = preferredLanguage,
                    ContactMethod = (i % 3 == 0) ? ContactMethod.Text : (i % 3 == 1) ? ContactMethod.Phone : ContactMethod.InPerson,
                    InterestLevel = interest,
                    StudyLocationType = (i % 6 == 0) ? StudyLocationType.Public : StudyLocationType.Home,
                    Region = (i % 2 == 0) ? "Monroe Co" : "Nearby",
                    Notes = BuildPlausibleNotes(rng),
                    Status = status,
                    Age = rng.Next(18, 72),
                    Gender = gender,
                    IsDeleted = false
                });
            }

            return results;
        }

        private static string BuildPlausibleAddress(Random rng, string town)
        {
            // "Plausible not geocoded": looks right, not necessarily real.
            var streets = new[]
            {
                "Main", "Maple", "Oak", "Cedar", "Magnolia", "Walnut", "Hillside", "Church", "River", "Spring"
            };
            var suffix = new[] { "Rd", "St", "Ave", "Ln", "Dr", "Pike", "Hwy" };

            var number = rng.Next(101, 9999);
            var street = Pick(rng, streets);
            var suf = Pick(rng, suffix);

            // Kentucky ZIPs: we keep them "plausible looking" without pretending they’re accurate.
            // You can swap these out later if you want more realism.
            var zip = rng.Next(42100, 42799);

            return $"{number} {street} {suf}, {town}, KY {zip}";
        }

        private static string BuildPlausibleNotes(Random rng)
        {
            var notes = new[]
            {
                "Met at the door; friendly conversation. Follow up on brochure.",
                "Interested in family topics. Mentioned Bible study schedule might work.",
                "Prefers evenings. Has questions about suffering and God's Kingdom.",
                "Busy schedule; suggested a short return visit next week.",
                "Enjoys reading. Asked about resurrection hope."
            };
            return Pick(rng, notes);
        }

        // -------------------------------------------------------------------------------------------------------------
        // VISITS
        // -------------------------------------------------------------------------------------------------------------

        private static List<Visit> BuildSeedVisits(Random rng, List<Student> students, DateTime now)
        {
            // One future visit per student is the norm; we’ll seed for most, not all.
            // This gives you:
            // - students with visits (calendar dots + agenda)
            // - students without visits (empty day states + scheduling)
            var results = new List<Visit>();

            // Create visits for ~70% of students
            var studentsWithVisits = students
                .Where((s, idx) => idx % 10 != 0 && s.Status == StudentStatus.Active)
                .ToList();

            foreach (var s in studentsWithVisits)
            {
                var daysAhead = rng.Next(1, 35); // within ~5 weeks
                var hour = rng.Next(9, 19);      // 9am–6pm
                var minute = (rng.Next(0, 4) * 15); // 0/15/30/45

                var scheduled = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0)
                    .AddDays(daysAhead)
                    .AddHours(hour)
                    .AddMinutes(minute);

                var visitType = (s.InterestLevel == InterestLevel.Study)
                    ? VisitType.BibleStudy
                    : VisitType.ReturnVisit;

                var v = new Visit
                {
                    StudentId = s.StudentId,
                    ScheduledDateTime = scheduled,
                    Status = VisitStatus.Scheduled,
                    VisitType = visitType,
                    Notes = $"Follow-up with {s.Name.Split(' ')[0]}.",
                };

                // About 25% get an override location (coffee shop / park / etc.)
                if (rng.NextDouble() < 0.25)
                {
                    v.LocationAddressOverride = BuildPlausibleMeetup(rng);
                    var (olat, olon) = RandomPointWithinMiles(rng, s.StudyLatitude ?? CenterLat, s.StudyLongitude ?? CenterLon, 8.0);
                    v.LocationLatitudeOverride = olat;
                    v.LocationLongitudeOverride = olon;
                }

                results.Add(v);
            }

            // Make sure visits are time-ordered-ish for nicer initial calendar UX
            results.Sort((a, b) => a.ScheduledDateTime.CompareTo(b.ScheduledDateTime));

            return results;
        }

        private static string BuildPlausibleMeetup(Random rng)
        {
            var places = new[]
            {
                "Coffee shop parking lot",
                "City park pavilion",
                "Library entrance",
                "Grocery store lot (by pharmacy)",
                "Gas station (front bench)"
            };

            return Pick(rng, places) + ", Monroe County area";
        }

        // -------------------------------------------------------------------------------------------------------------
        // GEO: Random point within radius (miles) around a center (lat/lon)
        // -------------------------------------------------------------------------------------------------------------

        private static (double lat, double lon) RandomPointWithinMiles(Random rng, double centerLat, double centerLon, double radiusMiles)
        {
            // Uniform distribution inside a circle:
            // r = R * sqrt(u), theta = 2πv
            var u = rng.NextDouble();
            var v = rng.NextDouble();

            var r = radiusMiles * Math.Sqrt(u);
            var theta = 2.0 * Math.PI * v;

            // Convert miles to degrees (approx; perfect for dev seeding)
            const double milesPerDegLat = 69.0;
            var milesPerDegLon = milesPerDegLat * Math.Cos(centerLat * Math.PI / 180.0);

            var dLat = (r * Math.Cos(theta)) / milesPerDegLat;
            var dLon = (r * Math.Sin(theta)) / milesPerDegLon;

            return (centerLat + dLat, centerLon + dLon);
        }

        private static T Pick<T>(Random rng, IReadOnlyList<T> items)
            => items[rng.Next(items.Count)];
    }
}
