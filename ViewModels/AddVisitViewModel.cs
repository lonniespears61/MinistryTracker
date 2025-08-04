using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class AddVisitViewModel : ObservableObject
    {
        private readonly DataService _dataService;
        private readonly int _studentId;

        public AddVisitViewModel(DataService dataService, int studentId)
        {
            _dataService = dataService;
            _studentId = studentId;
            LoadDefaults();
        }

        // 👇 Default selected visit type (can be changed by user in UI)
        [ObservableProperty]
        private VisitType selectedVisitType = VisitType.ReturnVisit;

        // 👇 The actual scheduled date & time
        [ObservableProperty]
        private DateTime scheduledDateTime = DateTime.Now;

        // 👇 Optional visit notes (talking points, reminders, etc.)
        [ObservableProperty]
        private string note = string.Empty;

        // 👇 Optional reason if the visit is cancelled (future use)
        [ObservableProperty]
        private string? cancellationReason;

        /*
        ❌ Removed this — VisitStatus should be an enum, but we don't need to bind/edit it for now.
        If you want to allow status editing later, uncomment this with the correct type.
        
        [ObservableProperty]
        private VisitStatus visitStatus = VisitStatus.Scheduled;
        */

        // 🔄 Called in constructor to set initial state
        private void LoadDefaults()
        {
            ScheduledDateTime = DateTime.Now;
        }

        // ✅ Save command (called when user taps "Save")
        [RelayCommand]
        private async Task SaveVisitAsync()
        {
            var visit = new Visit
            {
                StudentId = _studentId,
                ScheduledDateTime = ScheduledDateTime,
                Status = VisitStatus.Scheduled,          // ✅ Correct enum usage
                VisitType = SelectedVisitType,
                Note = Note,
                CancellationReason = CancellationReason
            };

            await _dataService.AddVisitAsync(visit);
            await Shell.Current.GoToAsync(".."); // or use Navigation.PopAsync() if you're not using Shell
        }
    }
}
