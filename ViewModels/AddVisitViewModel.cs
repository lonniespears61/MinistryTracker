// ---------------------------------------------------------------------------------------------------------------------
// AddVisitViewModel.cs
//
// PURPOSE
// - ViewModel for AddVisitPage.
// - Receives studentId from Shell query.
// - Creates a scheduled Visit for that student.
// - Provides display fields used by AddVisitPage.xaml.
//
// NOTES
// - Uses Shell navigation.
// - StudentName is optional display text, but must exist for compiled binding.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MinistryTracker.Data;
using MinistryTracker.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class AddVisitViewModel : ObservableObject, IQueryAttributable
    {
        private readonly DataService _data;

        public AddVisitViewModel(DataService data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));

            SaveCommand = new AsyncRelayCommand(SaveVisitAsync);

            VisitDate = DateTime.Today;
            VisitTime = DateTime.Now.TimeOfDay;
        }

        [ObservableProperty]
        private int studentId;

        [ObservableProperty]
        private DateTime visitDate;

        [ObservableProperty]
        private TimeSpan visitTime;

        [ObservableProperty]
        private string? notes;

        [ObservableProperty]
        private string? studentName;

        [ObservableProperty]
        private bool isBusy;

        public IAsyncRelayCommand SaveCommand { get; }

        public async void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("studentId", out var raw) && raw is not null)
            {
                if (raw is int id)
                {
                    StudentId = id;
                }
                else if (raw is string s && int.TryParse(s, out var parsed))
                {
                    StudentId = parsed;
                }
            }

            await LoadStudentNameAsync();
        }

        public void Reset()
        {
            VisitDate = DateTime.Today;
            VisitTime = DateTime.Now.TimeOfDay;
            Notes = null;
        }

        private async Task LoadStudentNameAsync()
        {
            if (StudentId <= 0)
            {
                StudentName = "Adding Visit";
                return;
            }

            var student = await _data.GetStudentByIdAsync(StudentId);

            StudentName = student is null
                ? "Adding Visit"
                : $"Visit for {student.Name}";
        }

        private async Task SaveVisitAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                if (StudentId <= 0)
                    throw new InvalidOperationException("Visit.StudentId must be set before inserting a visit.");

                var visit = new Visit
                {
                    StudentId = StudentId,
                    ScheduledDateTime = VisitDate.Date + VisitTime,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
                };

                await _data.AddVisitAsync(visit);

                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}