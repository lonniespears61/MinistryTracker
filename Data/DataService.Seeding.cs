// ---------------------------------------------------------------------------------------------------------------------
// DataService.Seeding.cs  (DROP-IN)
// Plausible, deterministic seed data for UI testing (students + visits).
//
// GOAL:
// - "Realistic enough" Students + Visits for UI testing:
//     - list/search/filter
//     - profile/edit
//     - calendar grid + agenda
//     - near-me map (GPS clustering)
//
// KEY RULES (per your spec):
// - Students are labeled to these areas only:
//     - Monroe County, KY (central)  ✅ included
//     - Barren County, KY (NW)
//     - Metcalfe County, KY (NE)
//     - Cumberland County, KY (E)
//     - Allen County, KY (W)
//     - Clay County, TN (SE)
//     - Macon County, TN (SW)
// - ALL seeded GPS points are within 50 miles of Tompkinsville, KY.
// - Addresses are plausible strings (NOT geocoded).
// - We seed at most ONE future visit per student (most students get one).
// - Uses EnsureInitThen + CancellationToken.
//
// NOTES:
// - sqlite-net will set Student.StudentId and Visit.Id on insert when AutoIncrement is used.
// - This file ONLY seeds when there are no non-deleted students (safe default).
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
        // Approx: 36.699508, -85.692005
        private const double CenterLat = 36.699508;
        private const double CenterLon = -85.692005;

        // Hard rule: keep all seeded points within 50 miles of Tompkinsville.
        private const double MaxMilesFromCenter = 50.0;

        // Keep dev seeds deterministic so results are repeatable.
        private const int SeedRandom = 42;

        /// <summary>
        /// Seeds demo data (safe default).
        /// Does nothing if real (non-deleted) students already exist.
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
                // Seed Students
                // ---------------------------------------------------------------------
                var students = BuildSeedStudents(rng);

                foreach (var s in students)
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(s).ConfigureAwait(false);
                }

                // ---------------------------------------------------------------------
                // Seed Visits (at most one FUTURE scheduled visit per student)
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
            // Allowed seed areas (county + directional label + representative town)
            // Town/ZIP are used only as plausible address labels (NOT geocoded).
            var areas = new[]
            {
                // Monroe County, KY (central)
                new SeedArea("Monroe (Central)", "Tompkinsville", "KY", 42167, 42167),
                new SeedArea("Monroe (Central)", "Gamaliel",      "KY", 42140, 42140),
                new SeedArea("Monroe (Central)", "Hestand",       "KY", 42151, 42151),

                // Barren County, KY (NW)
                new SeedArea("Barren (NW)",      "Glasgow",       "KY", 42141, 42142),
                new SeedArea("Barren (NW)",      "Cave City",     "KY", 42127, 42127),

                // Metcalfe County, KY (NE)
                new SeedArea("Metcalfe (NE)",    "Edmonton",      "KY", 42129, 42129),

                // Cumberland County, KY (E)
                new SeedArea("Cumberland (E)",   "Burkesville",   "KY", 42717, 42717),

                // Allen County, KY (W)
                new SeedArea("Allen (W)",        "Scottsville",   "KY", 42164, 42164),

                // Clay County, TN (SE)
                new SeedArea("Clay (TN) (SE)",   "Celina",        "TN", 38551, 38551),

                // Macon County, TN (SW)
                new SeedArea("Macon (TN) (SW)",  "Lafayette",     "TN", 37083, 37083),
            };

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

            // Enough to test scrolling/search/filtering
            const int count = 20;

            var results = new List<Student>(capacity: count);

            for (int i = 0; i < count; i++)
            {
                var area = Pick(rng, areas);

                var name = $"{Pick(rng, firstNames)} {Pick(rng, lastNames)}";

                // GPS: hard rule enforced here (within MaxMilesFromCenter of Tompkinsville)
                var (lat, lon) = RandomPointWithinMiles(rng, CenterLat, CenterLon, MaxMilesFromCenter);

                var address = BuildPlausibleAddress(rng, area.Town, area.State, area.ZipMin, area.ZipMax);

                // Mix English/Spanish for early testing (even before localization UI)
                var preferredLanguage = (i % 5 == 0) ? "Spanish" : "English";

                // Status variety (mostly Active)
                var status = (i % 8 == 0) ? StudentStatus.Paused : StudentStatus.Active;

                // Interest variety
                var interest = (i % 7 == 0) ? InterestLevel.Potential
                             : (i % 4 == 0) ? InterestLevel.Interested
                             : InterestLevel.Study;

                // Call type variety
                var callType = (i % 4 == 0) ? InitialCallType.HouseToHouse
                             : (i % 4 == 1) ? InitialCallType.Cart
                             : (i % 4 == 2) ? InitialCallType.Phone
                             : InitialCallType.Letter;

                Gender? gender =
                    (i % 3 == 0) ? Gender.Female :
                    (i % 3 == 1) ? Gender.Male :
                    (Gender?)null;

                results.Add(new Student
                {
                    Name = name,
                    CallType = callType,
                    FirstContactDate = DateTime.Today.AddDays(-rng.Next(7, 140)),

                    StudyAddress = address,
                    StudyLatitude = lat,
                    StudyLongitude = lon,

                    PreferredLanguage = preferredLanguage,
                    ContactMethod = (i % 3 == 0) ? ContactMethod.Text
                                   : (i % 3 == 1) ? ContactMethod.Phone
                                   : ContactMethod.InPerson,

                    InterestLevel = interest,
                    StudyLocationType = (i % 6 == 0) ? StudyLocationType.Public : StudyLocationType.Home,

                    // Your UI can filter by these region labels
                    Region = area.CountyLabel,

                    Notes = BuildPlausibleNotes(rng),
                    Status = status,

                    Age = rng.Next(18, 72),
                    Gender = gender,

                    IsDeleted = false
                });
            }

            return results;
        }

        private static string BuildPlausibleAddress(Random rng, string town, string state, int zipMin, int zipMax)
        {
            var streets = new[]
            {
                "Main", "Maple", "Oak", "Cedar", "Magnolia",
                "Walnut", "Hillside", "Church", "River", "Spring"
            };

            var suffix = new[] { "Rd", "St", "Ave", "Ln", "Dr", "Pike", "Hwy" };

            var number = rng.Next(101, 9999);
            var street = Pick(rng, streets);
            var suf = Pick(rng, suffix);

            var zip = (zipMin == zipMax) ? zipMin : rng.Next(zipMin, zipMax + 1);

            return $"{number} {street} {suf}, {town}, {state} {zip}";
        }

        private static string BuildPlausibleNotes(Random rng)
        {
            var notes = new[]
            {
                "Met at the door; friendly conversation. Follow up on brochure.",
                "Interested in family topics. Mentioned Bible study schedule might work.",
                "Prefers evenings. Has questions about suffering and God's Kingdom.",
                "Busy schedule; suggested a short return visit next week.",
                "Enjoys reading. Asked about resurrection hope.",
                "Asked about prayer and why God allows wickedness."
            };

            return Pick(rng, notes);
        }

        // -------------------------------------------------------------------------------------------------------------
        // VISITS
        // -------------------------------------------------------------------------------------------------------------

        private static List<Visit> BuildSeedVisits(Random rng, List<Student> students, DateTime now)
        {
            // Seed visits for most ACTIVE students, but not all.
            // This gives you:
            // - students with scheduled visits (calendar)
            // - students without scheduled visits (empty states + scheduling flows)
            var results = new List<Visit>();

            var studentsWithVisits = students
                .Where((s, idx) => idx % 10 != 0 && s.Status == StudentStatus.Active)
                .ToList();

            foreach (var s in studentsWithVisits)
            {
                var daysAhead = rng.Next(1, 35);       // within ~5 weeks
                var hour = rng.Next(9, 19);            // 9am–6pm
                var minute = rng.Next(0, 4) * 15;      // 0/15/30/45

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
                    Notes = $"Follow-up with {FirstWordOrFallback(s.Name, "student")}.",
                };

                // About 25% get an override location (coffee shop / park / etc.)
                if (rng.NextDouble() < 0.25)
                {
                    v.LocationAddressOverride = BuildPlausibleMeetup(rng);

                    // Small override cluster around the student (still realistic)
                    var baseLat = s.StudyLatitude ?? CenterLat;
                    var baseLon = s.StudyLongitude ?? CenterLon;
                    var (olat, olon) = RandomPointWithinMiles(rng, baseLat, baseLon, 8.0);

                    v.LocationLatitudeOverride = olat;
                    v.LocationLongitudeOverride = olon;
                }

                results.Add(v);
            }

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

            // Keep it “local-ish” without claiming a real address.
            return $"{Pick(rng, places)} (local area)";
        }

        private static string FirstWordOrFallback(string? text, string fallback)
        {
            if (string.IsNullOrWhiteSpace(text)) return fallback;
            var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : fallback;
        }

        // -------------------------------------------------------------------------------------------------------------
        // GEO: Random point within radius (miles) around a center (lat/lon)
        // -------------------------------------------------------------------------------------------------------------

        private static (double lat, double lon) RandomPointWithinMiles(
            Random rng,
            double centerLat,
            double centerLon,
            double radiusMiles)
        {
            // Uniform distribution inside a circle:
            // r = R * sqrt(u), theta = 2πv
            var u = rng.NextDouble();
            var v = rng.NextDouble();

            var r = radiusMiles * Math.Sqrt(u);
            var theta = 2.0 * Math.PI * v;

            // Convert miles to degrees (approx; great for dev seeding)
            const double milesPerDegLat = 69.0;
            var milesPerDegLon = milesPerDegLat * Math.Cos(centerLat * Math.PI / 180.0);

            var dLat = (r * Math.Cos(theta)) / milesPerDegLat;
            var dLon = (r * Math.Sin(theta)) / milesPerDegLon;

            return (centerLat + dLat, centerLon + dLon);
        }

        private static T Pick<T>(Random rng, IReadOnlyList<T> items)
            => items[rng.Next(items.Count)];

        private readonly record struct SeedArea(
            string CountyLabel,
            string Town,
            string State,
            int ZipMin,
            int ZipMax);
    }
}
