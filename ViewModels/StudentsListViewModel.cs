// ---------------------------------------------------------------------------------------------------------------------
// StudentsListViewModel.cs
//
// PURPOSE
// - Orchestrates the student list: load, filter, and schedule visit flow.
//
// DESIGN RULES
// - ViewModel owns data/state logic
// - View owns UI prompts and navigation execution
// - Scheduling conflicts are surfaced to the View through an event/callback pattern
// - ViewModel must not directly call UI display APIs
//
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using MinistryTracker.Data;
using MinistryTracker.Data.Repositories;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels;

public partial class StudentsListViewModel : ObservableObject
{
    private readonly IStudentRepository _students;
    private readonly IVisitRepository _visits;
    private readonly ILogger<StudentsListViewModel>? _log;

    public ObservableCollection<StudentViewModel> Students { get; } = new();

    [ObservableProperty]
    private ObservableCollection<StudentViewModel> filteredStudents = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private bool isActiveOnly = true;
    [ObservableProperty] private bool isBusy;

    public int VisibleCount => FilteredStudents.Count;

    public StudentsListViewModel(
        IStudentRepository students,
        IVisitRepository visits,
        ILogger<StudentsListViewModel>? log = null)
    {
        _students = students;
        _visits = visits;
        _log = log;
    }

    // =========================================================================
    // EVENTS
    // =========================================================================

    /// <summary>
    /// Fired when a student already has a scheduled visit.
    /// Parameters: conflict message, student id, existing visit id, callback(choice).
    /// The View shows an action sheet and calls the callback with the chosen option.
    /// </summary>
    public event Action<string, int, int, Action<string?>>? ScheduleConflictDetected;

    /// <summary>
    /// Fired when the VM wants navigation to occur.
    /// The View performs the actual navigation.
    /// </summary>
    public event Action<string>? RequestNavigate;

    // =========================================================================
    // LOAD
    // =========================================================================

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var allStudents = await _students.GetStudentsAsync().ConfigureAwait(false);

            var tasks = allStudents.Select(async student =>
            {
                var next = await _visits.GetNextFutureVisitForStudentAsync(student.StudentId)
                                      .ConfigureAwait(false);
                return (Student: student, NextVisit: next);
            });

            var enriched = await Task.WhenAll(tasks).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Students.Clear();

                foreach (var item in enriched)
                {
                    var svm = new StudentViewModel(item.Student);

                    if (item.NextVisit is not null)
                    {
                        svm.NextFutureVisitId = item.NextVisit.Id;
                        svm.NextFutureVisitDisplay = $"Next: {item.NextVisit.ScheduledDateTime:g}";
                    }
                    else
                    {
                        svm.NextFutureVisitId = null;
                        svm.NextFutureVisitDisplay = "No visit scheduled";
                    }

                    Students.Add(svm);
                }

                ApplyFilter();
            });
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "Failed to load students.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // =========================================================================
    // REFRESH
    // =========================================================================

    [RelayCommand]
    private async Task Refresh()
        => await LoadAsync();

    // =========================================================================
    // SCHEDULE VISIT
    // =========================================================================

    [RelayCommand]
    private async Task ScheduleVisit(StudentViewModel? svm)
    {
        if (svm?.Model is null) return;

        if (svm.Model.IsDeleted || svm.Model.Status != StudentStatus.Active)
            return;

        try
        {
            var studentId = svm.Model.StudentId;
            var conflict = await _visits
                .GetVisitScheduleConflictAsync(studentId)
                .ConfigureAwait(false);

            if (conflict is null)
            {
                RequestNavigate?.Invoke($"AddVisitPage?studentId={studentId}");
                return;
            }

            var existing = conflict.Visit;
            var when = existing.ScheduledDateTime;
            var message = $"Scheduled for {when:ddd, MMM d} at {when:h:mm tt}.";

            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

            ScheduleConflictDetected?.Invoke(message, studentId, existing.Id, choice =>
            {
                tcs.TrySetResult(choice);
            });

            var choice = await tcs.Task.ConfigureAwait(false);

            switch (choice)
            {
                case "Edit existing":
                    RequestNavigate?.Invoke($"UpdateVisitPage?visitId={existing.Id}");
                    break;

                case "Replace it":
                    RequestNavigate?.Invoke(
                        $"AddVisitPage?studentId={studentId}&replaceVisitId={existing.Id}");
                    break;

                    // "Cancel" or null = do nothing
            }
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "ScheduleVisit failed.");
        }
    }

    // =========================================================================
    // FILTERING
    // =========================================================================

    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnIsActiveOnlyChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(ApplyFilter);
            return;
        }

        var term = (SearchText ?? string.Empty).Trim();
        IEnumerable<StudentViewModel> query = Students;

        if (IsActiveOnly)
            query = query.Where(s => s.Model.Status == StudentStatus.Active);

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(s =>
                (!string.IsNullOrEmpty(s.Name) &&
                 s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(s.PhoneNumber) &&
                 s.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        query = query.OrderBy(s => s.Name ?? string.Empty);

        FilteredStudents.Clear();

        var index = 0;
        foreach (var student in query)
        {
            student.IsAlternate = (index % 2 == 1);
            FilteredStudents.Add(student);
            index++;
        }

        OnPropertyChanged(nameof(VisibleCount));
    }
}
