using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using MinistryTracker.Data;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel for the student/visit map.
    ///
    /// DESIGN RULES
    /// - Student no longer owns interaction location
    /// - Visit owns meeting address / coordinates
    /// - Map pins are created from visits that have valid meeting coordinates
    /// </summary>
    public partial class StudentsMapViewModel : ObservableObject
    {
        private readonly DataService _data;

        public StudentsMapViewModel(DataService data)
        {
            _data = data;
            RefreshPinsCommand = new AsyncRelayCommand(RefreshPinsAsync);
        }

        /// <summary>
        /// Pins shown on the map.
        /// </summary>
        public ObservableCollection<Pin> Pins { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? status;

        public IAsyncRelayCommand RefreshPinsCommand { get; }

        /// <summary>
        /// Loads upcoming visits with meeting coordinates and creates map pins.
        /// </summary>
        private async Task RefreshPinsAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                Status = "Loading visit locations…";

                Pins.Clear();

                // Pull upcoming visits, then pin only the ones with coordinates.
                var visits = await _data.GetUpcomingVisitsAsync();

                foreach (var v in visits)
                {
                    if (v.MeetingLatitude is null || v.MeetingLongitude is null)
                        continue;

                    var student = await _data.GetStudentByIdAsync(v.StudentId);

                    var pin = new Pin
                    {
                        Label = student?.Name ?? "Unknown",
                        Address = v.MeetingAddress ?? string.Empty,
                        Type = PinType.Place,
                        Location = new Location(v.MeetingLatitude.Value, v.MeetingLongitude.Value)
                    };

                    Pins.Add(pin);
                }

                Status = $"Pinned: {Pins.Count}";
            }
            catch (Exception ex)
            {
                Status = $"Map load failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}