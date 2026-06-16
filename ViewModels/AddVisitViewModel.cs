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
using System.Globalization;
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

        [ObservableProperty]
        private string? returnTo;

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

            if (query.TryGetValue("date", out var rawDate) && rawDate is not null)
            {
                if (rawDate is DateTime date)
                {
                    VisitDate = date.Date;
                }
                else if (rawDate is string s &&
                         (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ||
                          DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed)))
                {
                    VisitDate = parsed.Date;
                }
            }

            if (query.TryGetValue("returnTo", out var rawReturnTo) && rawReturnTo is not null)
            {
                ReturnTo = rawReturnTo.ToString();
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

                if (string.Equals(ReturnTo, "calendar", StringComparison.OrdinalIgnoreCase))
                {
                    await Shell.Current.GoToAsync($"//{MinistryTracker.AppShell.CalendarTabRoute}");
                    return;
                }

                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
