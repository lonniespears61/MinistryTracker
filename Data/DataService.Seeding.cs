// ---------------------------------------------------------------------------------------------------------------------
// DataService.Seeding.cs
//
// PURPOSE
// - Deterministic beta test data covering current student and visit workflows.
// - Uses fictional people and estimated locations within about 50 miles of Tompkinsville, Kentucky.
//
// DESIGN RULES
// - Each named seed scenario exists for a specific screen or business rule.
// - Dates are relative to today so dashboard and calendar scenarios stay useful.
// - Reschedule links are inserted in dependency order so history is realistic.
// - Addresses are intentionally fictional test addresses.
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

                var studentsByKey = new Dictionary<string, Student>(StringComparer.Ordinal);

                foreach (var definition in BuildSeedStudents())
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(definition.Student).ConfigureAwait(false);
                    studentsByKey.Add(definition.Key, definition.Student);
                }

                var visitsByKey = new Dictionary<string, Visit>(StringComparer.Ordinal);

                foreach (var definition in BuildSeedVisits(DateTime.Now))
                {
                    ct.ThrowIfCancellationRequested();

                    var visit = definition.Visit;
                    visit.StudentId = studentsByKey[definition.StudentKey].StudentId;

                    if (definition.RescheduledFromKey is not null)
                    {
                        visit.RescheduledFromVisitId =
                            visitsByKey[definition.RescheduledFromKey].Id;
                    }

                    await Db.InsertAsync(visit).ConfigureAwait(false);
                    visitsByKey.Add(definition.Key, visit);
                }
            }, ct);
        }

        // =====================================================================
        // STUDENTS
        // =====================================================================

        private static IReadOnlyList<SeedStudentDefinition> BuildSeedStudents()
        {
            return new[]
            {
                Student(
                    "today",
                    "Maria Today",
                    "101 Test Route, Tompkinsville, KY 42167",
                    36.7021,
                    -85.6915,
                    StudentStatus.Active,
                    InitialContactType.HouseToHouse,
                    ContactMethod.InPerson,
                    "Scheduled today; validates the Dashboard Today section and map."),

                Student(
                    "upcoming-map",
                    "James Upcoming",
                    "202 Sample Lane, Gamaliel, KY 42140",
                    36.6400,
                    -85.7934,
                    StudentStatus.Active,
                    InitialContactType.Cart,
                    ContactMethod.InPerson,
                    "Upcoming mapped visit and future calendar coverage."),

                Student(
                    "recent-missed",
                    "Linda Followup",
                    "303 Demo Road, Tompkinsville, KY 42167",
                    36.7160,
                    -85.6740,
                    StudentStatus.Active,
                    InitialContactType.Phone,
                    ContactMethod.Phone,
                    "Recent missed visit with no notes or replacement."),

                Student(
                    "older-missed",
                    "Carlos Overdue",
                    "404 Example Hollow, Fountain Run, KY 42133",
                    36.7189,
                    -85.9657,
                    StudentStatus.Active,
                    InitialContactType.Letter,
                    ContactMethod.Letter,
                    "Older missed visit for the dashboard overdue bucket."),

                Student(
                    "missed-followup",
                    "Ashley Followup Set",
                    "505 Test Creek Road, Gamaliel, KY 42140",
                    36.6550,
                    -85.7700,
                    StudentStatus.Active,
                    InitialContactType.SMPW,
                    ContactMethod.Text,
                    "Missed visit with a future replacement; should not appear unresolved."),

                Student(
                    "checkon-old",
                    "Robert Checkon",
                    "606 Sample Street, Burkesville, KY 42717",
                    36.7904,
                    -85.3706,
                    StudentStatus.Active,
                    InitialContactType.Other,
                    ContactMethod.InPerson,
                    "Last successful visit was more than 30 days ago."),

                Student(
                    "checkon-none",
                    "Sofia No Visits",
                    "707 Demo Avenue, Tompkinsville, KY 42167",
                    36.6900,
                    -85.7100,
                    StudentStatus.Active,
                    InitialContactType.Cart,
                    ContactMethod.WhatsApp,
                    "No visits yet; should be eligible for Let's Check On."),

                Student(
                    "recent-success",
                    "Miguel Recent",
                    "808 Example Drive, Tompkinsville, KY 42167",
                    36.7350,
                    -85.7050,
                    StudentStatus.Active,
                    InitialContactType.HouseToHouse,
                    ContactMethod.Email,
                    "Recent successful visit; should not be a check-on suggestion."),

                Student(
                    "paused",
                    "Karen Paused",
                    "909 Test Farm Road, Fountain Run, KY 42133",
                    36.7050,
                    -85.9300,
                    StudentStatus.Paused,
                    InitialContactType.Phone,
                    ContactMethod.Phone,
                    "Paused student; excluded from active suggestions."),

                Student(
                    "discontinued",
                    "Ethan Discontinued",
                    "110 Sample Ridge, Scottsville, KY 42164",
                    36.7533,
                    -86.1905,
                    StudentStatus.Discontinued,
                    InitialContactType.Letter,
                    ContactMethod.Letter,
                    "Discontinued student retained for history testing."),

                Student(
                    "completed",
                    "Rosa Completed",
                    "211 Demo Parkway, Burkesville, KY 42717",
                    36.7750,
                    -85.4050,
                    StudentStatus.Completed,
                    InitialContactType.SMPW,
                    ContactMethod.InPerson,
                    "Completed student retained for history testing."),

                Student(
                    "canceled",
                    "Daniel Canceled",
                    "312 Example Circle, Tompkinsville, KY 42167",
                    36.6800,
                    -85.6600,
                    StudentStatus.Active,
                    InitialContactType.Other,
                    ContactMethod.Text,
                    "Contains both cancellation outcomes."),

                Student(
                    "rescheduled",
                    "Nora Rescheduled",
                    "413 Test Loop, Gamaliel, KY 42140",
                    36.6250,
                    -85.8100,
                    StudentStatus.Active,
                    InitialContactType.Cart,
                    ContactMethod.InPerson,
                    "Contains an upcoming reschedule history chain."),

                Student(
                    "missed-rescheduled",
                    "Samuel Missed Followup",
                    "514 Sample Crossing, Tompkinsville, KY 42167",
                    36.7480,
                    -85.6500,
                    StudentStatus.Active,
                    InitialContactType.Phone,
                    ContactMethod.Phone,
                    "Missed outcome remains intact while linked to a future follow-up."),

                Student(
                    "history",
                    "Olivia History",
                    "615 Demo Trail, Scottsville, KY 42164",
                    36.7300,
                    -86.1300,
                    StudentStatus.Active,
                    InitialContactType.HouseToHouse,
                    ContactMethod.InPerson,
                    "Multiple visits and contact methods for student history testing."),

                Student(
                    "conflict",
                    "Peter Conflict",
                    "716 Example Way, Tompkinsville, KY 42167",
                    36.7100,
                    -85.7300,
                    StudentStatus.Active,
                    InitialContactType.Other,
                    ContactMethod.InPerson,
                    "Future visit provides a known time for 30-minute conflict testing."),

                Student(
                    "failed-geocode",
                    "Grace Address Only",
                    "817 Test Highway, Fountain Run, KY 42133",
                    null,
                    null,
                    StudentStatus.Active,
                    InitialContactType.Letter,
                    ContactMethod.Letter,
                    "Address-only student for missing coordinate behavior.",
                    GeocodeStatus.Failed),

                Student(
                    "soft-deleted",
                    "Deleted Test Student",
                    "918 Sample Road, Tompkinsville, KY 42167",
                    36.7000,
                    -85.7000,
                    StudentStatus.Active,
                    InitialContactType.Other,
                    ContactMethod.Other,
                    "Soft-deleted record should stay out of normal lists.",
                    isDeleted: true)
            };
        }

        private static SeedStudentDefinition Student(
            string key,
            string name,
            string address,
            double? latitude,
            double? longitude,
            StudentStatus status,
            InitialContactType initialContactType,
            ContactMethod preferredContactMethod,
            string notes,
            GeocodeStatus geocodeStatus = GeocodeStatus.Success,
            bool isDeleted = false)
        {
            return new SeedStudentDefinition(
                key,
                new Student
                {
                    Name = name,
                    InitialContactType = initialContactType,
                    FirstContactDate = DateTime.Today.AddDays(-90),
                    PhoneNumber = $"270-555-{StablePhoneSuffix(key):0000}",
                    Email = $"{key}@example.test",
                    PrimaryAddress = address,
                    IsHomeAddress = true,
                    LocationContext = LocationContext.Home,
                    PrimaryLatitude = latitude,
                    PrimaryLongitude = longitude,
                    PrimaryGeocodeStatus =
                        latitude is not null && longitude is not null
                            ? geocodeStatus
                            : GeocodeStatus.Failed,
                    PreferredLanguage = "English",
                    PreferredContactMethod = preferredContactMethod,
                    Status = status,
                    Notes = notes,
                    IsDeleted = isDeleted
                });
        }

        // =====================================================================
        // VISITS
        // =====================================================================

        private static IReadOnlyList<SeedVisitDefinition> BuildSeedVisits(DateTime now)
        {
            var todayVisitTime = NextUsableTimeToday(now);
            var conflictTime = DateTime.Today.AddDays(2).AddHours(10);

            return new[]
            {
                Visit(
                    "today-scheduled",
                    "today",
                    todayVisitTime,
                    VisitStatus.Scheduled,
                    ContactMethod.InPerson,
                    "101 Test Route, Tompkinsville, KY 42167",
                    36.7021,
                    -85.6915,
                    "Bring the requested publication."),

                Visit(
                    "upcoming-map",
                    "upcoming-map",
                    DateTime.Today.AddDays(3).AddHours(14),
                    VisitStatus.Scheduled,
                    ContactMethod.InPerson,
                    "202 Sample Lane, Gamaliel, KY 42140",
                    36.6400,
                    -85.7934,
                    "Map pin and upcoming calendar test."),

                Visit(
                    "recent-missed",
                    "recent-missed",
                    DateTime.Today.AddDays(-3).AddHours(16),
                    VisitStatus.Missed,
                    ContactMethod.Phone),

                Visit(
                    "older-missed",
                    "older-missed",
                    DateTime.Today.AddDays(-14).AddHours(11),
                    VisitStatus.Missed,
                    ContactMethod.Letter),

                Visit(
                    "missed-with-followup",
                    "missed-followup",
                    DateTime.Today.AddDays(-5).AddHours(13),
                    VisitStatus.Missed,
                    ContactMethod.Text),

                Visit(
                    "missed-followup-future",
                    "missed-followup",
                    DateTime.Today.AddDays(4).AddHours(13),
                    VisitStatus.Scheduled,
                    ContactMethod.Text,
                    notes: "Follow-up created after the missed contact.",
                    rescheduledFromKey: "missed-with-followup"),

                Visit(
                    "checkon-old-success",
                    "checkon-old",
                    DateTime.Today.AddDays(-45).AddHours(10),
                    VisitStatus.Successful,
                    ContactMethod.InPerson,
                    completedDateTime: DateTime.Today.AddDays(-45).AddHours(10).AddMinutes(20),
                    notes: "Good conversation; no future visit currently scheduled."),

                Visit(
                    "recent-success",
                    "recent-success",
                    DateTime.Today.AddDays(-10).AddHours(15),
                    VisitStatus.Successful,
                    ContactMethod.Email,
                    completedDateTime: DateTime.Today.AddDays(-10).AddHours(15).AddMinutes(5),
                    notes: "Replied by email."),

                Visit(
                    "paused-history",
                    "paused",
                    DateTime.Today.AddDays(-60).AddHours(9),
                    VisitStatus.Successful,
                    ContactMethod.Phone,
                    completedDateTime: DateTime.Today.AddDays(-60).AddHours(9).AddMinutes(15),
                    notes: "Asked to pause contact for now."),

                Visit(
                    "discontinued-history",
                    "discontinued",
                    DateTime.Today.AddDays(-75).AddHours(11),
                    VisitStatus.CanceledByThem,
                    ContactMethod.Letter,
                    notes: "Requested no additional visits."),

                Visit(
                    "completed-history",
                    "completed",
                    DateTime.Today.AddDays(-40).AddHours(14),
                    VisitStatus.Successful,
                    ContactMethod.InPerson,
                    completedDateTime: DateTime.Today.AddDays(-40).AddHours(14).AddMinutes(30),
                    notes: "Relationship moved to completed status."),

                Visit(
                    "canceled-by-me",
                    "canceled",
                    DateTime.Today.AddDays(-12).AddHours(10),
                    VisitStatus.CanceledByMe,
                    ContactMethod.Other,
                    notes: "Canceled by user due to schedule change."),

                Visit(
                    "canceled-by-them",
                    "canceled",
                    DateTime.Today.AddDays(-8).AddHours(10),
                    VisitStatus.CanceledByThem,
                    ContactMethod.Text,
                    notes: "Student asked to cancel."),

                Visit(
                    "rescheduled-original",
                    "rescheduled",
                    DateTime.Today.AddDays(2).AddHours(9),
                    VisitStatus.Rescheduled,
                    ContactMethod.InPerson,
                    "413 Test Loop, Gamaliel, KY 42140",
                    36.6250,
                    -85.8100,
                    "Rescheduled: student requested a later day."),

                Visit(
                    "rescheduled-replacement",
                    "rescheduled",
                    DateTime.Today.AddDays(5).AddHours(9).AddMinutes(30),
                    VisitStatus.Scheduled,
                    ContactMethod.InPerson,
                    "413 Test Loop, Gamaliel, KY 42140",
                    36.6250,
                    -85.8100,
                    "Rescheduled: student requested a later day.",
                    rescheduledFromKey: "rescheduled-original"),

                Visit(
                    "missed-reschedule-original",
                    "missed-rescheduled",
                    DateTime.Today.AddDays(-6).AddHours(17),
                    VisitStatus.Missed,
                    ContactMethod.Phone,
                    notes: "Missed: no answer."),

                Visit(
                    "missed-reschedule-followup",
                    "missed-rescheduled",
                    DateTime.Today.AddDays(6).AddHours(17),
                    VisitStatus.Scheduled,
                    ContactMethod.Phone,
                    notes: "Rescheduled after missed call.",
                    rescheduledFromKey: "missed-reschedule-original"),

                Visit(
                    "history-old-success",
                    "history",
                    DateTime.Today.AddDays(-80).AddHours(10),
                    VisitStatus.Successful,
                    ContactMethod.InPerson,
                    completedDateTime: DateTime.Today.AddDays(-80).AddHours(10).AddMinutes(25),
                    notes: "Initial return visit."),

                Visit(
                    "history-phone-success",
                    "history",
                    DateTime.Today.AddDays(-35).AddHours(18),
                    VisitStatus.Successful,
                    ContactMethod.Phone,
                    completedDateTime: DateTime.Today.AddDays(-35).AddHours(18).AddMinutes(10),
                    notes: "Short phone conversation."),

                Visit(
                    "history-recent-missed-noted",
                    "history",
                    DateTime.Today.AddDays(-4).AddHours(12),
                    VisitStatus.Missed,
                    ContactMethod.WhatsApp,
                    notes: "Message sent; awaiting reply."),

                Visit(
                    "history-future",
                    "history",
                    DateTime.Today.AddDays(7).AddHours(12),
                    VisitStatus.Scheduled,
                    ContactMethod.InPerson,
                    "615 Demo Trail, Scottsville, KY 42164",
                    36.7300,
                    -86.1300,
                    "Future visit after several prior attempts."),

                Visit(
                    "conflict-anchor",
                    "conflict",
                    conflictTime,
                    VisitStatus.Scheduled,
                    ContactMethod.InPerson,
                    "716 Example Way, Tompkinsville, KY 42167",
                    36.7100,
                    -85.7300,
                    "Try scheduling another visit between 9:31 and 10:29 to test conflict blocking."),

                Visit(
                    "editable-past-success",
                    "today",
                    DateTime.Today.AddDays(-20).AddHours(13),
                    VisitStatus.Successful,
                    ContactMethod.Text,
                    completedDateTime: DateTime.Today.AddDays(-20).AddHours(13).AddMinutes(5),
                    notes: "Past visit available for outcome and detail correction testing."),

                Visit(
                    "editable-past-scheduled",
                    "upcoming-map",
                    DateTime.Today.AddDays(-1).AddHours(10),
                    VisitStatus.Scheduled,
                    ContactMethod.InPerson,
                    notes: "Intentionally unresolved past scheduled visit; mark Successful or Missed.")
            };
        }

        private static SeedVisitDefinition Visit(
            string key,
            string studentKey,
            DateTime scheduledDateTime,
            VisitStatus status,
            ContactMethod method,
            string? meetingAddress = null,
            double? meetingLatitude = null,
            double? meetingLongitude = null,
            string? notes = null,
            DateTime? completedDateTime = null,
            string? rescheduledFromKey = null)
        {
            return new SeedVisitDefinition(
                key,
                studentKey,
                rescheduledFromKey,
                new Visit
                {
                    Method = method,
                    ScheduledDateTime = scheduledDateTime,
                    Status = status,
                    CompletedDateTime = completedDateTime,
                    MeetingAddress = meetingAddress,
                    MeetingLatitude = meetingLatitude,
                    MeetingLongitude = meetingLongitude,
                    Notes = notes,
                    NotesCreatedDateTime =
                        string.IsNullOrWhiteSpace(notes)
                            ? null
                            : scheduledDateTime <= DateTime.Now
                                ? scheduledDateTime.AddMinutes(15)
                                : DateTime.Now
                });
        }

        private static DateTime NextUsableTimeToday(DateTime now)
        {
            var candidate = now.AddMinutes(30);
            return candidate.Date == now.Date
                ? candidate
                : DateTime.Today.AddDays(1).AddTicks(-1);
        }

        private static int StablePhoneSuffix(string key)
        {
            var value = 17;

            foreach (var character in key)
                value = ((value * 31) + character) % 9000;

            return 1000 + value;
        }

        private sealed record SeedStudentDefinition(string Key, Student Student);

        private sealed record SeedVisitDefinition(
            string Key,
            string StudentKey,
            string? RescheduledFromKey,
            Visit Visit);
    }
}
