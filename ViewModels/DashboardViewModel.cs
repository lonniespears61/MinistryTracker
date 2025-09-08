// ViewModels/DashboardViewModel.cs
using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.DTOs;   // VisitWithStudent
using MinistryTracker.Models.Enums;  // StudentStatus, VisitStatus

namespace MinistryTracker.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        // Add these properties inside the class:
        [ObservableProperty] private string dbFilePath = string.Empty;
        [ObservableProperty] private string dbFileName = string.Empty;

        private readonly DataService _data;

        // ---------- Cards / counters ----------
        [ObservableProperty] private int activeStudentsCount;

        [ObservableProperty] private int thisWeekScheduledCount;
        [ObservableProperty] private int thisWeekCompletedCount;
        [ObservableProperty] private int thisWeekCanceledCount;

        // ---------- Lists ----------
        // Full list of scheduled visits for the current week
        public ObservableCollection<VisitWithStudent> ThisWeekScheduled { get; } = new();
        // Optional: top 5 upcoming from "now"
        public ObservableCollection<VisitWithStudent> ThisWeekUpcoming { get; } = new();

        [ObservableProperty] private bool isBusy;

        public DashboardViewModel(DataService data) => _data = data;

        // Disable double-taps; keeps the UI from starting two loads at once
        [RelayCommand(AllowConcurrentExecutions = false)]
        public async Task LoadAsync()
        {

            // At the very top of LoadAsync()
            var path = _data.GetDatabasePath();
            DbFilePath = path;
            DbFileName = Path.GetFileName(path);

            if (IsBusy) return;


            try
            {
                IsBusy = true;

                // --- Students ---
                // Prefer a clear rule: Active only. If your Student model uses IsDeleted instead,
                // change the predicate to: s => !s.IsDeleted
                var students = await _data.GetStudentsAsync().ConfigureAwait(false);
                ActiveStudentsCount = students.Count(s => s.Status == StudentStatus.Active);

                // --- Date bounds for "this week" (Sunday..Saturday) ---
                // Adjust if you prefer Monday as first day of week.
                var today = DateTime.Today;
                int diff = (7 + (int)today.DayOfWeek - (int)DayOfWeek.Sunday) % 7;
                var weekStart = today.AddDays(-diff);            // Sunday 00:00
                var weekEnd = weekStart.AddDays(7);            // next Sunday 00:00 (exclusive)

                // ---- Visits this week (all statuses) ----
                // Prefer a single “flattened” query that already joins students.
                // If you don’t have these methods in DataService yet, see notes below.
                var allThisWeek = await _data
                    .GetVisitsWithStudentsInRangeAsync(weekStart, weekEnd)
                    .ConfigureAwait(false);

                ThisWeekScheduledCount = allThisWeek.Count(v => v.Status == VisitStatus.Scheduled);
                ThisWeekCompletedCount = allThisWeek.Count(v => v.Status == VisitStatus.Completed);
                ThisWeekCanceledCount = allThisWeek.Count(v => v.Status == VisitStatus.Canceled);

                // --- Build "ThisWeekScheduled": scheduled only, ordered by time ---
                var scheduled = allThisWeek
                    .Where(x => x.Status == VisitStatus.Scheduled)
                    .OrderBy(x => x.ScheduledDateTime)
                    .ToList();

                ThisWeekScheduled.Clear();
                foreach (var v in scheduled)
                    ThisWeekScheduled.Add(v);

                // --- Optional: top 5 upcoming from *now* ---
                var now = DateTime.Now;
                var upcoming = scheduled
                    .Where(x => x.ScheduledDateTime >= now)
                    .Take(5)
                    .ToList();

                ThisWeekUpcoming.Clear();
                foreach (var v in upcoming)
                    ThisWeekUpcoming.Add(v);
            }
            catch (Exception ex)
            {
                // Don’t crash the page; you can surface ex.Message via a toast if you like
                ThisWeekScheduled.Clear();
                ThisWeekUpcoming.Clear();
                ThisWeekScheduledCount = ThisWeekCompletedCount = ThisWeekCanceledCount = 0;
                ActiveStudentsCount = 0;

#if DEBUG
                System.Diagnostics.Debug.WriteLine($"Dashboard LoadAsync error: {ex}");
#endif
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
