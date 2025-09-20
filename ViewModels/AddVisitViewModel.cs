using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;

namespace MinistryTracker.ViewModels
{
    public partial class AddVisitViewModel : ObservableObject
    {
        private readonly DataService _data;

        [ObservableProperty] private int studentId;

        // Inline, no-popup state:
        [ObservableProperty] private DateTime displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        [ObservableProperty] private DateTime visitDate = DateTime.Today;
        [ObservableProperty] private TimeSpan visitTime = RoundToNearest(DateTime.Now.TimeOfDay, 15);

        public ObservableCollection<DayCell> Days { get; } = new();
        public ObservableCollection<TimeSpan> TimeSlots { get; } = new();

        public AddVisitViewModel(DataService data)
        {
            _data = data;
            BuildMonth(DisplayMonth);
            BuildTimeSlots(stepMinutes: 30); // visible “chips” every 30 mins by default
        }

        public void Load(Student student)
        {
            StudentId = student.StudentId;
            // default selects “today” and a rounded current time
            VisitDate = DateTime.Today;
            VisitTime = RoundToNearest(DateTime.Now.TimeOfDay, 15);
            SelectDate(VisitDate);
        }

        [RelayCommand] private void PrevMonth() => ChangeMonth(-1);
        [RelayCommand] private void NextMonth() => ChangeMonth(1);
        [RelayCommand]
        private void SelectDay(DayCell cell)
        {
            if (cell is null) return;
            VisitDate = cell.Date.Date;
            foreach (var d in Days) d.IsSelected = false;
            cell.IsSelected = true;
            OnPropertyChanged(nameof(Days));
        }
        [RelayCommand] private void SelectTime(TimeSpan t) => VisitTime = t;

        [RelayCommand]
        private async Task SaveAsync()
        {
            var when = VisitDate.Date + VisitTime;
            var visit = new Visit
            {
                StudentId = StudentId,
                ScheduledDateTime = when,
            };
            await _data.AddVisitAsync(visit);

            // Non-Shell navigation: App uses NavigationPage → PopAsync
            var nav = Application.Current?.MainPage?.Navigation;
            if (nav is not null) await nav.PopAsync();
        }

        private void ChangeMonth(int delta)
        {
            DisplayMonth = DisplayMonth.AddMonths(delta);
            BuildMonth(DisplayMonth);
        }

        private void BuildMonth(DateTime monthFirst)
        {
            Days.Clear();

            var first = new DateTime(monthFirst.Year, monthFirst.Month, 1);
            int offset = ((int)first.DayOfWeek + 6) % 7; // Monday=0 if you prefer; change to Sunday-first if needed
            var gridStart = first.AddDays(-offset);

            for (int i = 0; i < 42; i++) // 7×6 grid
            {
                var date = gridStart.AddDays(i);
                var cell = new DayCell
                {
                    Date = date,
                    IsCurrentMonth = (date.Month == monthFirst.Month),
                    IsSelected = date.Date == VisitDate.Date
                };
                Days.Add(cell);
            }
        }

        private void SelectDate(DateTime date)
        {
            foreach (var d in Days) d.IsSelected = (d.Date.Date == date.Date);
            OnPropertyChanged(nameof(Days));
        }

        private void BuildTimeSlots(int stepMinutes)
        {
            TimeSlots.Clear();
            var start = new TimeSpan(6, 0, 0);  // 6:00 AM
            var end = new TimeSpan(22, 0, 0); // 10:00 PM
            for (var t = start; t <= end; t = t.Add(TimeSpan.FromMinutes(stepMinutes)))
                TimeSlots.Add(t);
        }

        private static TimeSpan RoundToNearest(TimeSpan t, int minutes)
        {
            var block = TimeSpan.FromMinutes(minutes).Ticks;
            var blocks = (long)Math.Round((double)t.Ticks / block, MidpointRounding.AwayFromZero);
            return new TimeSpan(blocks * block);
        }
    }
}
