using CommunityToolkit.Mvvm.ComponentModel;

namespace MinistryTracker.Models
{
    public partial class DayCell : ObservableObject
    {
        public DateTime Date { get; set; }

        public int DayNumber => Date.Day;

        [ObservableProperty] private bool isCurrentMonth;
        [ObservableProperty] private bool isSelected;
        public bool IsToday => Date.Date == DateTime.Today;
    }
}
