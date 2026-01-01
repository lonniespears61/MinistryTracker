using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using MinistryTracker.Models.Enums;   // StudentStatus
using MinistryTracker.Data;

namespace MinistryTracker.ViewModels;

public partial class StudentsListViewModel : ObservableObject
{
    private readonly DataService _data;
    private readonly ILogger<StudentsListViewModel>? _log;

    // Master list (unfiltered). We never query the DB during search/filter.
    public ObservableCollection<StudentViewModel> Students { get; } = new();

    // The list the UI binds to (filtered view).
    [ObservableProperty]
    private ObservableCollection<StudentViewModel> filteredStudents = new();

    // Bound to the SearchBar
    [ObservableProperty]
    private string? searchText;

    // ✅ default ON to mirror Dashboard behavior
    [ObservableProperty]
    private bool isActiveOnly = true;

    [ObservableProperty]
    private bool isBusy;

    // Handy for showing counts in the UI if desired
    public int VisibleCount => FilteredStudents.Count;

    public StudentsListViewModel(DataService data, ILogger<StudentsListViewModel>? log = null)
    {
        _data = data;
        _log = log;
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // ---------------------------------------------------------------------------------
            // STEP 1: Fetch all students from DB.
            // ---------------------------------------------------------------------------------
            var allStudents = await _data.GetStudentsAsync().ConfigureAwait(false);

            // ---------------------------------------------------------------------------------
            // STEP 2: For each student, fetch their next future visit.
            //
            // WHY:
            // - UX rule: "Tap student = edit existing future visit if present; else schedule new."
            // - We prepare that info here so the UI does NOT do DB calls on tap.
            //
            // PERFORMANCE:
            // - Small dataset (<= 50 students): acceptable to prefetch.
            // - Use Task.WhenAll so we don't await 50 calls sequentially.
            //
            // NOTE:
            // - sqlite-net-pcl can handle this at your scale.
            // - If you ever scale up, we'd switch to a single bulk query.
            // ---------------------------------------------------------------------------------
            var nextVisitTasks = allStudents.Select(async s =>
            {
                var next = await _data.GetNextFutureVisitForStudentAsync(s.StudentId).ConfigureAwait(false);
                return (Student: s, NextVisit: next);
            });

            var enriched = await Task.WhenAll(nextVisitTasks).ConfigureAwait(false);

            // ---------------------------------------------------------------------------------
            // STEP 3: Update observable collections on UI thread.
            // ---------------------------------------------------------------------------------
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Students.Clear();

                foreach (var item in enriched)
                {
                    var svm = new StudentViewModel(item.Student);

                    // -------------------------------------------------------------------------
                    // NEW: next-visit fields on each row VM
                    //
                    // StudentViewModel must expose these properties (we'll add them next if needed):
                    //   int? NextFutureVisitId { get; set; }
                    //   string NextFutureVisitDisplay { get; set; }
                    //
                    // WHY:
                    // - Lets the Page decide navigation:
                    //     if NextFutureVisitId != null => UpdateVisitPage?visitId=...
                    //     else => AddVisitPage?studentId=...
                    // - UI can also show a hint like "Next: Jan 12" right in the list.
                    // -------------------------------------------------------------------------

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

                // Apply current search/active filter to produce FilteredStudents.
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

    // 🔄 Pull-to-refresh
    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }

    // When user types or toggles Active Only, we filter locally (NO DB calls here).
    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnIsActiveOnlyChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        // This method might be invoked from property setters on any context.
        // Keep UI mutations on the UI thread.
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(ApplyFilter);
            return;
        }

        var term = (SearchText ?? string.Empty).Trim();

        IEnumerable<StudentViewModel> query = Students;

        if (IsActiveOnly)
        {
            query = query.Where(s => s.Model.Status == StudentStatus.Active);
        }

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(s =>
                (!string.IsNullOrEmpty(s.Name) &&
                 s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||

                (!string.IsNullOrEmpty(s.Model.PreferredLanguage) &&
                 s.Model.PreferredLanguage.Contains(term, StringComparison.OrdinalIgnoreCase))
            );
        }

        // Optional: stable sort for nicer UX
        query = query.OrderBy(s => s.Name ?? string.Empty);

        // Replace contents without swapping the collection instance
        FilteredStudents.Clear();
        foreach (var s in query)
            FilteredStudents.Add(s);

        // Let the UI know counts changed
        OnPropertyChanged(nameof(VisibleCount));
    }
}
