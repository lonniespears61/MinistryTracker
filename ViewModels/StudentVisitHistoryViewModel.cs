using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MinistryTracker.Data.Repositories;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.ViewModels;

public partial class StudentVisitHistoryViewModel : ObservableObject
{
    private readonly IStudentRepository _students;
    private readonly IVisitRepository _visits;

    [ObservableProperty]
    private string studentName = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<VisitHistoryItemViewModel> Visits { get; } = new();

    public StudentVisitHistoryViewModel(
        IStudentRepository students,
        IVisitRepository visits)
    {
        _students = students;
        _visits = visits;
    }

    public async Task<bool> LoadAsync(int studentId)
    {
        if (IsBusy)
            return false;

        try
        {
            IsBusy = true;

            var student = await _students.GetStudentByIdAsync(studentId);
            if (student is null)
                return false;

            var visits = await _visits.GetVisitsForStudentAsync(studentId);

            StudentName = student.Name ?? "(Unnamed)";
            Visits.Clear();

            foreach (var visit in visits)
                Visits.Add(new VisitHistoryItemViewModel(visit));

            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed class VisitHistoryItemViewModel
{
    public VisitHistoryItemViewModel(Visit visit)
    {
        VisitId = visit.Id;
        ScheduledDateTime = visit.ScheduledDateTime;
        Status = GetStatusDisplay(visit.Status);
        Method = visit.Method == ContactMethod.InPerson
            ? "In Person"
            : visit.Method.ToString();
        Notes = visit.Notes ?? string.Empty;
    }

    public int VisitId { get; }
    public DateTime ScheduledDateTime { get; }
    public string Status { get; }
    public string Method { get; }
    public string Notes { get; }
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    private static string GetStatusDisplay(VisitStatus status) =>
        status switch
        {
            VisitStatus.CanceledByMe => "Canceled (Me)",
            VisitStatus.CanceledByThem => "Canceled (Them)",
            _ => status.ToString()
        };
}
