using MinistryTracker.Data.Repositories;
using MinistryTracker.Models.Enums;
using MinistryTracker.Views;

namespace MinistryTracker.Services;

public sealed class VisitWorkflowCoordinator
{
    public const string CancelToCalendar = WorkflowRoutes.CancelToCalendar;
    public const string CancelToDashboard = WorkflowRoutes.CancelToDashboard;

    private readonly IStudentRepository _students;
    private readonly IVisitRepository _visits;

    public VisitWorkflowCoordinator(
        IStudentRepository students,
        IVisitRepository visits)
    {
        _students = students;
        _visits = visits;
    }

    public async Task BeginSchedulingAsync(
        Page host,
        int studentId,
        DateTime? selectedDate = null,
        string cancelTo = CancelToDashboard)
    {
        var student = await _students.GetStudentByIdAsync(studentId);

        if (student is null || student.IsDeleted)
        {
            await host.DisplayAlert("Schedule Visit", "Student not found.", "OK");
            return;
        }

        if (!WorkflowPolicy.CanScheduleStudent(student))
        {
            await host.DisplayAlert(
                "Schedule Visit",
                $"Visits can only be scheduled for active students. Current status: {student.Status}.",
                "OK");
            return;
        }

        var conflict = await _visits.GetVisitScheduleConflictAsync(studentId);
        int? replaceVisitId = null;

        if (conflict is not null)
        {
            var existing = conflict.Visit;
            var choice = await host.DisplayActionSheet(
                "Visit already scheduled",
                "Keep existing",
                null,
                "Edit existing",
                "Replace it");

            if (choice == "Edit existing")
            {
                await Shell.Current.GoToAsync(
                    WorkflowRoutes.UpdateVisit(existing.Id));
                return;
            }

            if (choice != "Replace it")
                return;

            replaceVisitId = existing.Id;
        }

        if (selectedDate is DateTime date)
        {
            await ContinueSchedulingAsync(
                studentId,
                date,
                replaceVisitId,
                cancelTo);
            return;
        }

        await Shell.Current.GoToAsync(
            WorkflowRoutes.ScheduleCalendar(studentId, replaceVisitId));
    }

    public Task ContinueSchedulingAsync(
        int studentId,
        DateTime date,
        int? replaceVisitId = null,
        string cancelTo = CancelToCalendar)
    {
        return Shell.Current.GoToAsync(
            WorkflowRoutes.AddVisit(studentId, date, cancelTo, replaceVisitId));
    }

    public Task SelectStudentForDateAsync(DateTime date) =>
        Shell.Current.GoToAsync(
            WorkflowRoutes.SelectStudent(date));

    public async Task CompleteNewVisitAsync()
    {
        await Shell.Current.Navigation.PopToRootAsync(false);
        await Shell.Current.GoToAsync($"//{AppShell.DashboardTabRoute}");
    }

    public async Task CancelNewVisitAsync(string? cancelTo)
    {
        if (string.Equals(cancelTo, CancelToCalendar, StringComparison.OrdinalIgnoreCase))
        {
            await Shell.Current.Navigation.PopToRootAsync(false);
            await Shell.Current.GoToAsync($"//{AppShell.CalendarTabRoute}");
            return;
        }

        if (string.Equals(cancelTo, CancelToDashboard, StringComparison.OrdinalIgnoreCase))
        {
            await GoToDashboardAsync();
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    public async Task CompleteExistingVisitAsync(int studentId)
    {
        await Shell.Current.Navigation.PopToRootAsync(false);
        await Shell.Current.GoToAsync(
            WorkflowRoutes.StudentProfile(studentId));
    }

    public async Task GoToDashboardAsync()
    {
        await Shell.Current.Navigation.PopToRootAsync(false);
        await Shell.Current.GoToAsync($"//{AppShell.DashboardTabRoute}");
    }
}
