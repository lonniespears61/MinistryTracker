// ---------------------------------------------------------------------------------------------------------------------
// SelectStudentForVisitViewModel.cs
// Search/select an active student before scheduling a visit for a preselected date.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.Logging;
using MinistryTracker.Data;
using MinistryTracker.Data.Repositories;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels;

public partial class SelectStudentForVisitViewModel : ObservableObject
{
    private readonly IStudentRepository _students;
    private readonly ILogger<SelectStudentForVisitViewModel>? _log;

    public ObservableCollection<StudentViewModel> Students { get; } = new();

    [ObservableProperty] private ObservableCollection<StudentViewModel> filteredStudents = new();
    [ObservableProperty] private string? searchText;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private DateTime visitDate = DateTime.Today;

    public string VisitDateTitle => $"Schedule for {VisitDate:dddd, MMMM d}";

    public SelectStudentForVisitViewModel(
        IStudentRepository students,
        ILogger<SelectStudentForVisitViewModel>? log = null)
    {
        _students = students ?? throw new ArgumentNullException(nameof(students));
        _log = log;
    }

    public async Task LoadAsync(DateTime visitDate)
    {
        if (IsBusy) return;

        VisitDate = visitDate.Date;
        OnPropertyChanged(nameof(VisitDateTitle));

        try
        {
            IsBusy = true;

            var students = await _students.GetStudentsAsync().ConfigureAwait(false);
            var activeStudents = students
                .Where(s => s.Status == StudentStatus.Active)
                .OrderBy(s => s.Name)
                .Select(s => new StudentViewModel(s))
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Students.Clear();

                foreach (var student in activeStudents)
                    Students.Add(student);

                ApplyFilter();
            });
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "Failed to load students for visit selection.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(ApplyFilter);
            return;
        }

        var term = (SearchText ?? string.Empty).Trim();
        IEnumerable<StudentViewModel> query = Students;

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(s =>
                (!string.IsNullOrEmpty(s.Name) &&
                 s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(s.Model.PreferredLanguage) &&
                 s.Model.PreferredLanguage.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredStudents.Clear();

        var index = 0;
        foreach (var student in query.OrderBy(s => s.Name))
        {
            student.IsAlternate = index % 2 == 1;
            FilteredStudents.Add(student);
            index++;
        }
    }
}
