using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly DataService _data;

        [ObservableProperty] private int activeStudentsCount;

        [ObservableProperty] private int thisWeekScheduledCount;
        [ObservableProperty] private int thisWeekCompletedCount;
        [ObservableProperty] private int thisWeekCanceledCount;

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

                // Active students count (safe default: non-deleted)
                var students = await _data.GetStudentsAsync();
                ActiveStudentsCount = students.Count(s => !s.IsDeleted);

                // Visits this week
                var allThisWeek = await _data.GetVisitsThisWeekAsync(includeCanceled: true);

                ThisWeekScheduledCount = allThisWeek.Count(v => v.Status == VisitStatus.Scheduled);
                ThisWeekCompletedCount = allThisWeek.Count(v => v.Status == VisitStatus.Completed);
                ThisWeekCanceledCount = allThisWeek.Count(v => v.Status == VisitStatus.Canceled);

                // Next 5 scheduled from now within the week
                var now = DateTime.Now;
                var upcoming = (await _data.GetVisitsWithStudentsThisWeekAsync(includeCanceled: false))
                               .Where(x => x.Visit.Status == VisitStatus.Scheduled && x.Visit.ScheduledDateTime >= now)
                               .OrderBy(x => x.Visit.ScheduledDateTime)
                               .Take(5)
                               .ToList();

                ThisWeekUpcoming.Clear();
                foreach (var item in upcoming)
                    ThisWeekUpcoming.Add(item);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
