using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;

namespace MinistryTracker.ViewModels
{
    public partial class AddVisitViewModel : ObservableObject
    {
        private readonly DataService _data;

        // ---------- Student context ----------
        [ObservableProperty] private int studentId;

        // ---------- Calendar (inline, no popup) ----------
        [ObservableProperty]
        private DateTime displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        [ObservableProperty]
        private DateTime visitDate = DateTime.Today;

        // Backing data for the calendar grid (7×6 = 42 cells)
        public ObservableCollection<DayCell> Days { get; } = new();

        // ---------- Time (spinner-style) ----------
        // Final combined time used for saving (kept in sync with the spinners)
        [ObservableProperty]
        private TimeSpan visitTime = RoundToNearest(DateTime.Now.TimeOfDay, 15);

        // Spinner sources: 12-hour clock and 5-minute increments (0..55)
        public ObservableCollection<int> HourItems12 { get; } =
            new(Enumerable.Range(1, 12)); // 1..12

        public ObservableCollection<int> MinuteItems { get; } =
            new(Enumerable.Range(0, 12).Select(i => i * 5)); // 0,5,10,...55

        // Spinner selections
        [ObservableProperty] private int selectedHour12 = 12;
        [ObservableProperty] private int selectedMinute; // 0..55 (5-min steps)
        [ObservableProperty] private bool isAm = true;

        // Also expose chip-style times if you still want to show them
        public ObservableCollection<TimeSpan> TimeSlots { get; } = new();

        // ---------- Commands ----------
        [RelayCommand] private void PrevMonth() => ChangeMonth(-1);
        [RelayCommand] private void NextMonth() => ChangeMonth(1);

        [RelayCommand]
        private void SelectDay(DayCell? cell)
        {
            if (cell is null) return;

            VisitDate = cell.Date.Date;

            // mark selection in the grid
            foreach (var d in Days) d.IsSelected = false;
            cell.IsSelected = true;
        }

        [RelayCommand] private void SelectTime(TimeSpan t) => VisitTime = t;

        [RelayCommand] private void SetAm() => IsAm = true;
        [RelayCommand] private void SetPm() => IsAm = false;

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

            // App uses NavigationPage, so PopAsync (not Shell)
            var nav = Application.Current?.MainPage?.Navigation;
            if (nav is not null)
                await nav.PopAsync();
        }

        // ---------- Lifecycle ----------
        public AddVisitViewModel(DataService data)
        {
            _data = data;

            BuildMonth(DisplayMonth);
            BuildTimeSlots(stepMinutes: 30);      // optional: visible time chips
            SyncSpinnersFromVisitTime();          // initialize spinners from VisitTime
        }

        public void Load(Student student)
        {
            StudentId = student.StudentId;

            VisitDate = DateTime.Today;
            VisitTime = RoundToNearest(DateTime.Now.TimeOfDay, 15);

            SyncSpinnersFromVisitTime();          // set spinner UI from VisitTime
            SelectDate(VisitDate);                // highlight today in grid
        }

        // ---------- Synchronization logic ----------
        // Triggered automatically when these properties change (source generator)
        partial void OnVisitTimeChanged(TimeSpan value) => SyncSpinnersFromVisitTime();
        partial void OnSelectedHour12Changed(int value) => SyncVisitTimeFromSpinners();
        partial void OnSelectedMinuteChanged(int value) => SyncVisitTimeFromSpinners();
        partial void OnIsAmChanged(bool value) => SyncVisitTimeFromSpinners();

        private void SyncSpinnersFromVisitTime()
        {
            var h24 = VisitTime.Hours;                 // 0..23
            IsAm = h24 < 12;

            var h12 = h24 % 12;
            SelectedHour12 = (h12 == 0) ? 12 : h12;    // 12-hour display
            SelectedMinute = VisitTime.Minutes - (VisitTime.Minutes % 5);
        }

        private void SyncVisitTimeFromSpinners()
        {
            var baseHour = SelectedHour12 % 12;        // 0..11
            var hour24 = baseHour + (IsAm ? 0 : 12);   // AM/PM
            VisitTime = new TimeSpan(hour24, SelectedMinute, 0);
        }

        // ---------- Calendar helpers ----------
        private void ChangeMonth(int delta)
        {
            DisplayMonth = DisplayMonth.AddMonths(delta);
            BuildMonth(DisplayMonth);
        }

        private void BuildMonth(DateTime monthFirst)
        {
            Days.Clear();

            var first = new DateTime(monthFirst.Year, monthFirst.Month, 1);

            // Monday-first calendar (matches headers Mon..Sun)
            int offset = ((int)first.DayOfWeek);

            // If you prefer Sunday-first headers, use: int offset = (int)first.DayOfWeek;

            var gridStart = first.AddDays(-offset);

            for (int i = 0; i < 42; i++) // 7×6 grid
            {
                var date = gridStart.AddDays(i);
                Days.Add(new DayCell
                {
                    Date = date,
                    IsCurrentMonth = (date.Month == monthFirst.Month),
                    IsSelected = date.Date == VisitDate.Date
                });
            }
        }

        private void SelectDate(DateTime date)
        {
            foreach (var d in Days) d.IsSelected = (d.Date.Date == date.Date);
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
