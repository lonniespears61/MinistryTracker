// AddVisitViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;           // Application, etc.
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

            // Sensible defaults
            VisitDate = DateTime.Today;
            VisitTime = DateTime.Now.TimeOfDay;
        }

        // --------------------------------------------------------------------
        // Shell Query (required)
        // --------------------------------------------------------------------

        [ObservableProperty] private int studentId; // REQUIRED

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Handles both "?studentId=123" (string) and int forms.
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
        }

        // --------------------------------------------------------------------
        // Properties bound from XAML
        // --------------------------------------------------------------------

        [ObservableProperty] private DateTime visitDate;   // Required (default today)
        [ObservableProperty] private TimeSpan visitTime;   // Required (default now)
        [ObservableProperty] private string? notes;        // Optional

        [ObservableProperty] private bool isBusy;

        // Commands
        public IAsyncRelayCommand SaveCommand { get; }

        // Called by page OnAppearing() if you want a clean slate for fields
        public void Reset()
        {
            VisitDate = DateTime.Today;
            VisitTime = DateTime.Now.TimeOfDay;
            Notes = null;
        }

        private async Task SaveVisitAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // This matches your exception and prevents silent bad inserts
                if (StudentId <= 0)
                    throw new InvalidOperationException("Visit.StudentId must be set before inserting a visit.");

                var visit = new Visit
                {
                    StudentId = StudentId, // ✅ critical
                    ScheduledDateTime = VisitDate.Date + VisitTime,
                    Notes = Notes
                };

                await _data.AddVisitAsync(visit);

                // If this page is opened via Shell route, pop via Shell navigation
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
