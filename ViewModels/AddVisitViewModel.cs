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
        [ObservableProperty] private DateTime scheduledDateTime = DateTime.Now;
        // ... any other fields you have

        public AddVisitViewModel(DataService data)
        {
            _data = data;
        }

        public void Load(Student student)
        {
            StudentId = student.StudentId;
            // Set other defaults as needed
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            var visit = new Visit
            {
                StudentId = StudentId,
                ScheduledDateTime = ScheduledDateTime,
                // ...
            };

            await _data.AddVisitAsync(visit);
            await Shell.Current.GoToAsync("..");
        }
    }
}
