using MinistryTracker.Models.Enums;

namespace MinistryTracker.Tests;

public sealed class CallLifecycleTests
{
    [Fact]
    public async Task Completed_study_archives_and_restores_as_manual_return_visit()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        await db.Service.SetCallStageAsync(student.StudentId, CallStage.BibleStudy);

        Assert.Equal(1, await db.Service.ArchiveCompletedStudyAsync(student.StudentId));
        Assert.Empty(await db.Service.GetStudentsAsync());
        Assert.Single(await db.Service.GetArchivedCallsAsync());

        Assert.Equal(1, await db.Service.RestoreCompletedStudyAsync(student.StudentId));
        var restored = await db.Service.GetStudentByIdAsync(student.StudentId);
        Assert.False(restored?.IsArchived);
        Assert.Equal(CallStage.ReturnVisit, restored?.Stage);
        Assert.Equal(StageAuthority.Manual, restored?.StageAuthority);
    }

    [Fact]
    public async Task Automatic_bible_study_below_cadence_moves_to_return_visit()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        await db.Service.SetCallStageAsync(
            student.StudentId,
            CallStage.BibleStudy,
            StageAuthority.Automatic);

        var raw = await db.OpenRaw().FindAsync<MinistryTracker.Models.Student>(student.StudentId);
        raw.StageChangedUtc = DateTime.UtcNow.AddDays(-31);
        await db.OpenRaw().UpdateAsync(raw);

        Assert.Equal(1, await db.Service.ApplyBibleStudyCadenceAsync(DateTime.UtcNow));
        var changed = await db.Service.GetStudentByIdAsync(student.StudentId);
        Assert.Equal(CallStage.ReturnVisit, changed?.Stage);
        Assert.Equal(StageAuthority.CadenceDowngrade, changed?.StageAuthority);
    }

    [Fact]
    public async Task Manual_bible_study_is_exempt_from_cadence_downgrade()
    {
        await using var db = await TestDatabase.CreateAsync();
        var student = await db.AddStudentAsync();
        await db.Service.SetCallStageAsync(student.StudentId, CallStage.BibleStudy);

        Assert.Equal(0, await db.Service.ApplyBibleStudyCadenceAsync(DateTime.UtcNow.AddDays(60)));
        Assert.Equal(CallStage.BibleStudy, (await db.Service.GetStudentByIdAsync(student.StudentId))?.Stage);
    }
}
