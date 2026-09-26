using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.Data;

public partial class DataService
{
    public Task<int> SetCallStageAsync(
        int studentId,
        CallStage stage,
        StageAuthority authority = StageAuthority.Manual,
        CancellationToken ct = default)
        => EnsureInitThen(async () =>
        {
            var student = await Db.FindAsync<Student>(studentId).ConfigureAwait(false);
            if (student is null || student.IsDeleted || student.IsArchived)
                return 0;

            UnprotectAfterRead(student);
            student.Stage = stage;
            student.StageAuthority = authority;
            student.StageChangedUtc = DateTime.UtcNow;
            ProtectForWrite(student);
            return await Db.UpdateAsync(student).ConfigureAwait(false);
        }, ct);

    public Task<int> ArchiveCompletedStudyAsync(int studentId, CancellationToken ct = default)
        => EnsureInitThen(async () =>
        {
            var student = await Db.FindAsync<Student>(studentId).ConfigureAwait(false);
            if (student is null || student.IsDeleted)
                return 0;

            UnprotectAfterRead(student);
            student.IsArchived = true;
            student.ArchiveReason = ArchiveReason.StudyCompleted;
            student.ArchivedUtc = DateTime.UtcNow;
            ProtectForWrite(student);
            return await Db.UpdateAsync(student).ConfigureAwait(false);
        }, ct);

    public Task<int> RestoreCompletedStudyAsync(int studentId, CancellationToken ct = default)
        => EnsureInitThen(async () =>
        {
            var student = await Db.FindAsync<Student>(studentId).ConfigureAwait(false);
            if (student is null || student.IsDeleted ||
                !student.IsArchived || student.ArchiveReason != ArchiveReason.StudyCompleted)
            {
                return 0;
            }

            UnprotectAfterRead(student);
            student.IsArchived = false;
            student.ArchiveReason = ArchiveReason.None;
            student.ArchivedUtc = null;
            student.Stage = CallStage.ReturnVisit;
            student.StageAuthority = StageAuthority.Manual;
            student.StageChangedUtc = DateTime.UtcNow;
            ProtectForWrite(student);
            return await Db.UpdateAsync(student).ConfigureAwait(false);
        }, ct);

    public Task<List<Student>> GetArchivedCallsAsync(CancellationToken ct = default)
        => EnsureInitThen(async () =>
        {
            var students = await Db.Table<Student>()
                .Where(student => !student.IsDeleted && student.IsArchived)
                .ToListAsync()
                .ConfigureAwait(false);
            return UnprotectStudents(students).OrderBy(student => student.Name).ToList();
        }, ct);

    public Task<int> ApplyBibleStudyCadenceAsync(DateTime utcNow, CancellationToken ct = default)
        => EnsureInitThen(async () =>
        {
            var cutoff = utcNow.AddDays(-30);
            var candidates = await Db.Table<Student>()
                .Where(student =>
                    !student.IsDeleted && !student.IsArchived &&
                    student.Stage == CallStage.BibleStudy &&
                    student.StageAuthority == StageAuthority.Automatic &&
                    student.StageChangedUtc <= cutoff)
                .ToListAsync()
                .ConfigureAwait(false);

            var changed = 0;
            foreach (var student in candidates)
            {
                var completedStudies = await Db.Table<Visit>()
                    .Where(visit =>
                        visit.StudentId == student.StudentId &&
                        !visit.IsDeleted &&
                        visit.Kind == VisitKind.BibleStudy &&
                        visit.Status == VisitStatus.Successful &&
                        visit.CompletedDateTime >= cutoff &&
                        visit.CompletedDateTime <= utcNow)
                    .CountAsync()
                    .ConfigureAwait(false);

                if (completedStudies >= 3)
                    continue;

                UnprotectAfterRead(student);
                student.Stage = CallStage.ReturnVisit;
                student.StageAuthority = StageAuthority.CadenceDowngrade;
                student.StageChangedUtc = utcNow;
                ProtectForWrite(student);
                changed += await Db.UpdateAsync(student).ConfigureAwait(false);
            }

            return changed;
        }, ct);
}
