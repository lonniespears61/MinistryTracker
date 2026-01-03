// ---------------------------------------------------------------------------------------------------------------------
// DataService.Seeding.cs (DROP-IN - NO force)
//
// PURPOSE:
// - Optional demo/dev seed data.
// - Safe by default (does nothing if real data exists).
// - Idempotent: safe to call multiple times.
//
// RULES:
// ✅ NO "force" parameter (per your rule)
// ✅ Uses EnsureInitThen + CancellationToken
// ✅ Honors soft-delete when deciding "is there real data?"
// ✅ No UI dependencies
// ---------------------------------------------------------------------------------------------------------------------

using System;
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
        /// Seeds demo data ONLY if the database has no non-deleted students.
        /// Safe to call multiple times.
        /// </summary>
        public Task SeedDemoDataAsync(CancellationToken ct = default)
        {
            return EnsureInitThen(async () =>
            {
                ct.ThrowIfCancellationRequested();

                // Guard: do NOT reseed if any "real" students exist.
                var existingCount = await Db.Table<Student>()
                    .Where(s => !s.IsDeleted)
                    .CountAsync()
                    .ConfigureAwait(false);

                if (existingCount > 0)
                    return;

                // -----------------------------
                // Seed Students
                // -----------------------------
                var students = new[]
                {
                    new Student
                    {
                        Name = "Maria Gonzalez",
                        CallType = InitialCallType.HouseToHouse,
                        FirstContactDate = DateTime.Today.AddDays(-45),
                        PreferredLanguage = "Spanish",
                        InterestLevel = InterestLevel.Study,
                        Status = StudentStatus.Active,
                        StudyLocationType = StudyLocationType.Home
                    },
                    new Student
                    {
                        Name = "James Wilson",
                        CallType = InitialCallType.Cart,
                        FirstContactDate = DateTime.Today.AddDays(-20),
                        PreferredLanguage = "English",
                        InterestLevel = InterestLevel.Interested,
                        Status = StudentStatus.Active,
                        StudyLocationType = StudyLocationType.Public
                    },
                    new Student
                    {
                        Name = "Linda Park",
                        CallType = InitialCallType.Phone,
                        FirstContactDate = DateTime.Today.AddDays(-90),
                        PreferredLanguage = "Korean",
                        InterestLevel = InterestLevel.Potential,
                        Status = StudentStatus.Paused,
                        StudyLocationType = StudyLocationType.Home
                    }
                };

                foreach (var s in students)
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(s).ConfigureAwait(false);
                }

                // -----------------------------
                // Seed Visits (ONE future visit per student max)
                // -----------------------------
                var now = DateTime.Now;

                var visits = new[]
                {
                    new Visit
                    {
                        StudentId = students[0].StudentId,
                        ScheduledDateTime = now.AddDays(2).AddHours(18),
                        VisitType = VisitType.BibleStudy,
                        Status = VisitStatus.Scheduled,
                        Notes = "Continue lesson on God's Kingdom."
                    },
                    new Visit
                    {
                        StudentId = students[1].StudentId,
                        ScheduledDateTime = now.AddDays(5).AddHours(10),
                        VisitType = VisitType.ReturnVisit,
                        Status = VisitStatus.Scheduled,
                        Notes = "Follow up on tract discussion."
                    }
                };

                foreach (var v in visits)
                {
                    ct.ThrowIfCancellationRequested();
                    await Db.InsertAsync(v).ConfigureAwait(false);
                }

            }, ct);
        }
    }
}
