namespace MinistryTracker.Models
{
    public class DayCell
    {
        public DateTime Date { get; set; }
        public int DayNumber => Date.Day;
        public bool IsCurrentMonth { get; set; }
        public bool IsToday => Date.Date == DateTime.Today;
        public bool IsSelected { get; set; }
    }
}
