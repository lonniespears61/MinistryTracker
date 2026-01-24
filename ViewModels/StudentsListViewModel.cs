// StudentsListViewModel.cs — Students list orchestration VM — 2026-01-24

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Extensions.Logging;
using MinistryTracker.Data;
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

    // Master list (unfiltered). We never query the DB during search/filter.
    public ObservableCollection<StudentViewModel> Students { get; } = new();

    // The list the UI binds to (filtered view).
    [ObservableProperty]
    private ObservableCollection<StudentViewModel> filteredStudents = new();

    // Bound to the SearchBar
    [ObservableProperty]
    private string? searchText;

    // Default ON to mirror Dashboard behavior
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

    // ---------------------------------------------------------------------
    // LOAD
    // ---------------------------------------------------------------------

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // STEP 1: Fetch all students
            var allStudents = await _data.GetStudentsAsync().ConfigureAwait(false);

            // STEP 2: Fetch next future visit for each student (parallel)
            var nextVisitTasks = allStudents.Select(async s =>
            {
                var next = await _data
                    .GetNextFutureVisitForStudentAsync(s.StudentId)
                    .ConfigureAwait(false);

                return (Student: s, NextVisit: next);
            });

            var enriched = await Task.WhenAll(nextVisitTasks).ConfigureAwait(false);

            // STEP 3: Update observable collections on UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Students.Clear();

                foreach (var item in enriched)
                {
                    var svm = new StudentViewModel(item.Student);

                    if (item.NextVisit is not null)
                    {
                        svm.NextFutureVisitId = item.NextVisit.Id;
                        svm.NextFutureVisitDisplay =
                            $"Next: {item.NextVisit.ScheduledDateTime:g}";
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

    // ---------------------------------------------------------------------
    // REFRESH
    // ---------------------------------------------------------------------

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }

    // ---------------------------------------------------------------------
    // FILTERING / SEARCH
    // ---------------------------------------------------------------------

    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnIsActiveOnlyChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        // Ensure UI-thread mutation
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

        // Stable sort for consistent UX
        query = query.OrderBy(s => s.Name ?? string.Empty);

        // Rebuild filtered list AND assign row alternation
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
