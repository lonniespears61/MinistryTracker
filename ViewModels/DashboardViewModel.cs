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
// - Show "Missed Recently" only when there are unresolved missed visits.
// - Keep sections quiet when there is nothing actionable.
// - Do not duplicate business rules already owned by DataService.
//
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.DTOs;

namespace MinistryTracker.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DataService _data;

        [ObservableProperty] private string dbFilePath = string.Empty;
        [ObservableProperty] private string dbFileName = string.Empty;
        [ObservableProperty] private bool isBusy;

        /// <summary>
        /// Today's scheduled visits.
        /// Only shown when this collection has items.
        /// </summary>
        public ObservableCollection<VisitWithStudent> TodayVisits { get; } = new();

        /// <summary>
        /// Missed visits from the last 7 days that still need attention.
        /// Only shown when this collection has items.
        /// </summary>
        public ObservableCollection<VisitWithStudent> MissedRecently { get; } = new();

        public bool HasTodayVisits => TodayVisits.Count > 0;
        public bool HasMissedRecently => MissedRecently.Count > 0;

        public DashboardViewModel(DataService data)
        {
            _data = data;
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            DbFilePath = _data.GetDatabasePath();
            DbFileName = Path.GetFileName(DbFilePath);

            try
            {
                IsBusy = true;

                await LoadTodayVisitsAsync().ConfigureAwait(false);
                await LoadMissedRecentlyAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TodayVisits.Clear();
                MissedRecently.Clear();

                OnPropertyChanged(nameof(HasTodayVisits));
                OnPropertyChanged(nameof(HasMissedRecently));

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Dashboard LoadAsync error: {ex}");
#endif
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

            var today = await _data
                .GetVisitsWithStudentsInRangeAsync(start, end, includeCanceled: false)
                .ConfigureAwait(false);

            var scheduledToday = today
                .Where(v => v.Status == Models.Enums.VisitStatus.Scheduled)
                .OrderBy(v => v.ScheduledDateTime)
                .ToList();

            TodayVisits.Clear();

            foreach (var visit in scheduledToday)
                TodayVisits.Add(visit);

            OnPropertyChanged(nameof(HasTodayVisits));
        }

        private async Task LoadMissedRecentlyAsync()
        {
            var missedVisits = await _data
                .GetUnhandledMissedVisitsAsync(days: 7)
                .ConfigureAwait(false);

            MissedRecently.Clear();

            if (missedVisits.Count == 0)
            {
                OnPropertyChanged(nameof(HasMissedRecently));
                return;
            }

            // Small dataset: load students once and match in memory.
            var students = await _data.GetStudentsAsync().ConfigureAwait(false);
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