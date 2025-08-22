using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using MinistryTracker.Data;
using MinistryTracker.Models.DTOs;   // ✅ for VisitWithStudent
using MinistryTracker.Models.Enums;  // ✅ for VisitStatus

namespace MinistryTracker.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DataService _data;

        [ObservableProperty] private int activeStudentsCount;

        [ObservableProperty] private int thisWeekScheduledCount;
        [ObservableProperty] private int thisWeekCompletedCount;
        [ObservableProperty] private int thisWeekCanceledCount;

        // ✅ New: full list of scheduled visits for the current week
        public ObservableCollection<VisitWithStudent> ThisWeekScheduled { get; } = new();

        // (Optional) Keep your “top 5 next from now” panel
        public ObservableCollection<VisitWithStudent> ThisWeekUpcoming { get; } = new();

        [ObservableProperty] private bool isBusy;

        public DashboardViewModel(DataService data) => _data = data;

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // --- Students ---
                var students = await _data.GetStudentsAsync();
                ActiveStudentsCount = students.Count(s => !s.IsDeleted); // or s.IsActive if you added it

                // --- Visits this week (all statuses) ---
                var allThisWeek = await _data.GetVisitsThisWeekAsync(includeCanceled: true);
                ThisWeekScheduledCount = allThisWeek.Count(v => v.Status == VisitStatus.Scheduled);
                ThisWeekCompletedCount = allThisWeek.Count(v => v.Status == VisitStatus.Completed);
                ThisWeekCanceledCount = allThisWeek.Count(v => v.Status == VisitStatus.Canceled);

                // --- Build "ThisWeekScheduled": scheduled only, ordered by time ---
                var flattened = await _data.GetVisitsWithStudentsThisWeekAsync(includeCanceled: false);

                var scheduled = flattened
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
            finally
            {
                IsBusy = false;
            }
        }
    }
}
