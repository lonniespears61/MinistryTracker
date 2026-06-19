// ---------------------------------------------------------------------------------------------------------------------
// DashboardViewModel.cs
//
// PURPOSE
// - Drives the dashboard as a decision-helper, not a reporting screen.
// - Surfaces only the people/visits that need attention now.
//
// DESIGN RULES
// - No counts or summary metrics.
// - Show today's scheduled visits only when they exist.
// - Show "Follow Up" only when there are unresolved missed visits.
// - Show "It's Been Awhile" for active students with no future visit and no
//   recorded visit in at least 30 days.
// - Keep sections quiet when there is nothing actionable.
// - Do not duplicate business rules already owned by the repositories.
//
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Data.Repositories;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IStudentRepository _students;
        private readonly IVisitRepository _visits;

        [ObservableProperty] private bool isBusy;

        /// <summary>
        /// Today's scheduled visits.
        /// Only shown when this collection has items.
        /// </summary>
        public ObservableCollection<VisitWithStudent> TodayVisits { get; } = new();

        /// <summary>
        /// Recent unresolved missed visits that still need attention.
        /// Only shown when this collection has items.
        /// </summary>
        public ObservableCollection<VisitWithStudent> MissedRecently { get; } = new();

        /// <summary>
        /// Active students with no future visit whose last visit was at least 30 days ago.
        /// Only shown when this collection has items.
        /// </summary>
        public ObservableCollection<CheckOnStudentSuggestion> LongOverdue { get; } = new();

        public bool HasTodayVisits => TodayVisits.Count > 0;
        public bool HasMissedRecently => MissedRecently.Count > 0;
        public bool HasLongOverdue => LongOverdue.Count > 0;

        public DashboardViewModel(IStudentRepository students, IVisitRepository visits)
        {
            _students = students;
            _visits = visits;
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                await LoadTodayVisitsAsync();
                await LoadMissedRecentlyAsync();
                await LoadLongOverdueAsync();
            }
            catch (Exception ex)
            {
                TodayVisits.Clear();
                MissedRecently.Clear();
                LongOverdue.Clear();

                OnPropertyChanged(nameof(HasTodayVisits));
                OnPropertyChanged(nameof(HasMissedRecently));
                OnPropertyChanged(nameof(HasLongOverdue));

                System.Diagnostics.Debug.WriteLine($"Dashboard LoadAsync error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadTodayVisitsAsync()
        {
            var start = DateTime.Today;
            var end = start.AddDays(1);

            var today = await _visits.GetVisitsWithStudentsInRangeAsync(start, end, includeCanceled: false);

            var scheduledToday = today
                .Where(v => v.Status == VisitStatus.Scheduled)
                .OrderBy(v => v.ScheduledDateTime)
                .ToList();

            TodayVisits.Clear();

            foreach (var visit in scheduledToday)
                TodayVisits.Add(visit);

            OnPropertyChanged(nameof(HasTodayVisits));
        }

        private async Task LoadMissedRecentlyAsync()
        {
            var missedVisits = await _visits.GetUnhandledMissedVisitsAsync(days: 7);

            MissedRecently.Clear();

            if (missedVisits.Count == 0)
            {
                OnPropertyChanged(nameof(HasMissedRecently));
                return;
            }

            // Small dataset: load students once and match in memory.
            var students = await _students.GetStudentsAsync();
            var studentsById = students.ToDictionary(s => s.StudentId);

            var projected = new List<VisitWithStudent>(missedVisits.Count);

            foreach (var visit in missedVisits.OrderByDescending(v => v.ScheduledDateTime))
            {
                studentsById.TryGetValue(visit.StudentId, out var student);

                projected.Add(new VisitWithStudent
                {
                    VisitId = visit.Id,
                    StudentId = visit.StudentId,
                    StudentName = student?.Name ?? "(Unnamed)",
                    ScheduledDateTime = visit.ScheduledDateTime,
                    Status = visit.Status,
                    NotesPreview = BuildNotesPreview(visit.Notes)
                });
            }

            foreach (var item in projected)
                MissedRecently.Add(item);

            OnPropertyChanged(nameof(HasMissedRecently));
        }

        private async Task LoadLongOverdueAsync()
        {
            var suggestions = await _students.GetCheckOnStudentSuggestionsAsync(
                take: 5,
                minDaysSinceVisit: 30);

            LongOverdue.Clear();

            foreach (var suggestion in suggestions)
                LongOverdue.Add(suggestion);

            OnPropertyChanged(nameof(HasLongOverdue));
        }

        private static string BuildNotesPreview(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return string.Empty;

            return notes.Length > 80
                ? notes[..80] + "…"
                : notes;
        }
    }
}
