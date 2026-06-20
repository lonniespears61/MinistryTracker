// ---------------------------------------------------------------------------------------------------------------------
// StudentsListViewModel.cs
//
// PURPOSE
// - Orchestrates student list loading and filtering.
//
// DESIGN RULES
// - ViewModel owns data/state logic
// - View owns UI prompts and navigation execution
// - Scheduling workflow is owned centrally by VisitWorkflowCoordinator
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
