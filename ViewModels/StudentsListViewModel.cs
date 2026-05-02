// ---------------------------------------------------------------------------------------------------------------------
// StudentsListViewModel.cs — Students list orchestration VM — 2026-05-02
//
// PURPOSE
// - Loads students for StudentsListPage.
// - Enriches each student with next scheduled visit info.
// - Handles search/filter behavior.
// - Handles schedule/edit/replace visit flow from the student list.
//
// NOTES
// - This project uses Shell navigation.
// - Do not use Application.Current.MainPage; it is obsolete in modern .NET MAUI.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
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

    public ObservableCollection<StudentViewModel> Students { get; } = new();

    [ObservableProperty]
    private ObservableCollection<StudentViewModel> filteredStudents = new();

    [ObservableProperty]
    private string? searchText;

    [ObservableProperty]
    private bool isActiveOnly = true;

    [ObservableProperty]
    private bool isBusy;

    public int VisibleCount => FilteredStudents.Count;

    public StudentsListViewModel(DataService data, ILogger<StudentsListViewModel>? log = null)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _log = log;
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var allStudents = await _data.GetStudentsAsync().ConfigureAwait(false);

            var nextVisitTasks = allStudents.Select(async student =>
            {
                var nextVisit = await _data
                    .GetNextFutureVisitForStudentAsync(student.StudentId)
                    .ConfigureAwait(false);

                return (Student: student, NextVisit: nextVisit);
            });

            var enrichedStudents = await Task.WhenAll(nextVisitTasks).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Students.Clear();

                foreach (var item in enrichedStudents)
                {
                    var studentViewModel = new StudentViewModel(item.Student);

                    if (item.NextVisit is not null)
                    {
                        studentViewModel.NextFutureVisitId = item.NextVisit.Id;
                        studentViewModel.NextFutureVisitDisplay = $"Next: {item.NextVisit.ScheduledDateTime:g}";
                    }
                    else
                    {
                        studentViewModel.NextFutureVisitId = null;
                        studentViewModel.NextFutureVisitDisplay = "No visit scheduled";
                    }

                    Students.Add(studentViewModel);
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

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ScheduleVisit(StudentViewModel? studentViewModel)
    {
        if (studentViewModel?.Model is null)
            return;

        try
        {
            var studentId = studentViewModel.Model.StudentId;

            var existingVisit = await _data
                .GetNextFutureVisitForStudentAsync(studentId)
                .ConfigureAwait(false);

            if (existingVisit is null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync($"AddVisitPage?studentId={studentId}");
                });

                return;
            }

            var scheduledTime = existingVisit.ScheduledDateTime;

            var choice = await MainThread.InvokeOnMainThreadAsync(() =>
                Shell.Current.DisplayActionSheet(
                    "Visit already scheduled",
                    "Cancel",
                    null,
                    "Edit existing",
                    "Replace it"));

            switch (choice)
            {
                case "Edit existing":
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Shell.Current.GoToAsync($"UpdateVisitPage?visitId={existingVisit.Id}");
                    });
                    break;

                case "Replace it":
                    await _data
                        .CancelVisitAsync(existingVisit.Id, reason: "Replaced by new scheduled visit")
                        .ConfigureAwait(false);

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Shell.Current.GoToAsync($"AddVisitPage?studentId={studentId}");
                    });
                    break;
            }
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "ScheduleVisit failed.");
        }
    }

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
        {
            query = query.Where(s => s.Model.Status == StudentStatus.Active);
        }

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(s =>
                (!string.IsNullOrEmpty(s.Name) &&
                 s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(s.Model.PreferredLanguage) &&
                 s.Model.PreferredLanguage.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        query = query.OrderBy(s => s.Name ?? string.Empty);

        FilteredStudents.Clear();

        var index = 0;

        foreach (var student in query)
        {
            student.IsAlternate = index % 2 == 1;
            FilteredStudents.Add(student);
            index++;
        }

        OnPropertyChanged(nameof(VisibleCount));
    }
}