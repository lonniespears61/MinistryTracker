// StudentsListViewModel.cs — Students list orchestration VM — 2026-03-28

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using MinistryTracker.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MinistryTracker.Models.Enums;

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

            var allStudents = await _data.GetStudentsAsync().ConfigureAwait(false);

            var nextVisitTasks = allStudents.Select(async s =>
            {
                var next = await _data
                    .GetNextFutureVisitForStudentAsync(s.StudentId)
                    .ConfigureAwait(false);

                return (Student: s, NextVisit: next);
            });

            var enriched = await Task.WhenAll(nextVisitTasks).ConfigureAwait(false);

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

    // ---------------------------------------------------------------------
    // REFRESH
    // ---------------------------------------------------------------------

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }

    // ---------------------------------------------------------------------
    // SCHEDULE VISIT
    // ---------------------------------------------------------------------

    [RelayCommand]
    private async Task ScheduleVisit(StudentViewModel? svm)
    {
        if (svm?.Model is null) return;

        try
        {
            var studentId = svm.Model.StudentId;

            var existing = await _data
                .GetNextFutureVisitForStudentAsync(studentId)
                .ConfigureAwait(false);

            if (existing is null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.GoToAsync($"AddVisitPage?studentId={studentId}");
                });
                return;
            }

            var when = existing.ScheduledDateTime;
            var message = $"This student already has a visit scheduled for {when:ddd, MMM d, yyyy} at {when:h:mm tt}.";

            var choice = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page is null) return "Cancel";

                return await page.DisplayActionSheet(
                    "Visit already scheduled",
                    "Cancel",
                    null,
                    "Edit existing",
                    "Replace it"
                );
            });

            switch (choice)
            {
                case "Edit existing":
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Shell.Current.GoToAsync($"EditVisitPage?visitId={existing.Id}");
                    });
                    break;

                case "Replace it":
                    await _data.CancelVisitAsync(existing.Id, "Replaced by new visit")
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

    // ---------------------------------------------------------------------
    // FILTERING
    // ---------------------------------------------------------------------

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

                (!string.IsNullOrEmpty(s.PhoneNumber) &&
                 s.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase))
            );
        }

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