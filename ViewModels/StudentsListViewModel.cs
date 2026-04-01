// ---------------------------------------------------------------------------------------------------------------------
// StudentsListViewModel.cs
// PURPOSE
// - Orchestrates the student list: load, filter, schedule visit flow.
//
// VIOLATION FIXED
// - ScheduleVisit was calling Application.Current.Windows.FirstOrDefault().Page.DisplayActionSheet(...)
//   directly. That is a UI call from a ViewModel. ViewModels must not reference Application,
//   Windows, Page, or any UI display methods.
//
// FIX APPROACH — event with callback pattern
// The VM fires a ScheduleConflictDetected event that carries the conflict details and a
// callback delegate. The View shows the action sheet and calls back with the user's choice.
// The VM then acts on that choice (cancel, edit, replace).
//
// WHY NOT MESSENGER?
// StudentsListPage and StudentsListViewModel have a direct 1:1 relationship — the page
// creates the VM via DI and holds a reference to it. An event is simpler and traceable.
// Messenger adds indirection that is only worth it for cross-page signals.
//
// WHY NOT JUST MOVE THE ACTIONSHEET INTO THE VIEW WITH A COMMAND PARAMETER?
// The conflict check (does this student have a future visit?) requires a DataService call.
// That data call belongs in the VM, not the View. So the VM must remain the entry point
// for the ScheduleVisit action, but it cannot show UI — hence the event.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using MinistryTracker.Data;
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
    private readonly DataService _data;
    private readonly ILogger<StudentsListViewModel>? _log;

    public ObservableCollection<StudentViewModel> Students { get; } = new();

    [ObservableProperty]
    private ObservableCollection<StudentViewModel> filteredStudents = new();

    [ObservableProperty] private string? searchText;
    [ObservableProperty] private bool isActiveOnly = true;
    [ObservableProperty] private bool isBusy;

    public int VisibleCount => FilteredStudents.Count;

    public StudentsListViewModel(DataService data, ILogger<StudentsListViewModel>? log = null)
    {
        _data = data;
        _log = log;
    }

    // =========================================================================
    // EVENT — fires when a scheduling conflict is detected
    //
    // WHY AN EVENT WITH A CALLBACK?
    // The VM needs the user's answer (Edit / Replace / Cancel) before it can
    // proceed. A Task-based approach or async event won't work cleanly here.
    // The callback pattern (Action<string?>) lets the View call back into the
    // VM's continuation without the VM ever touching UI.
    //
    // The View subscribes in OnAppearing, unsubscribes in OnDisappearing.
    // =========================================================================

    /// <summary>
    /// Fired when a student already has a scheduled visit.
    /// Parameters: conflict message, student id, existing visit id, callback(choice).
    /// The View shows an action sheet and calls the callback with the chosen option.
    /// </summary>
    public event Action<string, int, int, Action<string?>>? ScheduleConflictDetected;

    // =========================================================================
    // LOAD
    // =========================================================================

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            var allStudents = await _data.GetStudentsAsync().ConfigureAwait(false);

            // Enrich each student with their next scheduled visit in parallel
            var tasks = allStudents.Select(async s =>
            {
                var next = await _data.GetNextFutureVisitForStudentAsync(s.StudentId)
                                      .ConfigureAwait(false);
                return (Student: s, NextVisit: next);
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
    // REFRESH COMMAND
    // =========================================================================

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    // =========================================================================
    // SCHEDULE VISIT
    //
    // VIOLATION FIXED: no longer calls Application.Current or DisplayActionSheet.
    // If there is no conflict → navigation intent is communicated via RequestNavigate.
    // If there is a conflict → fires ScheduleConflictDetected for the View to handle.
    // =========================================================================

    /// <summary>
    /// Fired when the VM wants to navigate somewhere.
    /// The View calls Shell.Current.GoToAsync(route) in response.
    /// </summary>
    public event Action<string>? RequestNavigate;

    [RelayCommand]
    private async Task ScheduleVisit(StudentViewModel? svm)
    {
        if (svm?.Model is null) return;
        try
        {
            var studentId = svm.Model.StudentId;
            var existing = await _data.GetNextFutureVisitForStudentAsync(studentId)
                                      .ConfigureAwait(false);

            if (existing is null)
            {
                // No conflict — navigate directly to Add Visit
                RequestNavigate?.Invoke($"AddVisitPage?studentId={studentId}");
                return;
            }

            // Conflict: student already has a future visit.
            // VIOLATION FIXED: we do NOT call DisplayActionSheet here.
            // Fire an event; the View shows the dialog and calls back with the choice.
            var when = existing.ScheduledDateTime;
            var message = $"Scheduled for {when:ddd, MMM d} at {when:h:mm tt}.";

            // Use a TaskCompletionSource so we can await the user's choice
            // even though the event is synchronous.
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
                    await _data.CancelVisitAsync(existing.Id, "Replaced by new visit")
                               .ConfigureAwait(false);
                    RequestNavigate?.Invoke($"AddVisitPage?studentId={studentId}");
                    break;

                    // "Cancel" or null — do nothing
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
            query = query.Where(s =>
                (!string.IsNullOrEmpty(s.Name) &&
                 s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(s.PhoneNumber) &&
                 s.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase)));

        query = query.OrderBy(s => s.Name ?? string.Empty);

        FilteredStudents.Clear();
        var index = 0;
        foreach (var s in query)
        {
            s.IsAlternate = (index % 2 == 1);
            FilteredStudents.Add(s);
            index++;
        }

        OnPropertyChanged(nameof(VisibleCount));
    }
}
