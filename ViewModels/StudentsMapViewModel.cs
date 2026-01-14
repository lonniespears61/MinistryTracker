using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using MinistryTracker.Data;
using MinistryTracker.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class StudentsMapViewModel : ObservableObject
    {
        private readonly DataService _data;

        public StudentsMapViewModel(DataService data)
        {
            _data = data;

            RefreshPinsCommand = new AsyncRelayCommand(RefreshPinsAsync);
        }

        public ObservableCollection<Pin> Pins { get; } = new();

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? status;

        public IAsyncRelayCommand RefreshPinsCommand { get; }

        private async Task RefreshPinsAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                Status = "Loading students with locations…";

                Pins.Clear();

                var students = await _data.GetStudentsWithLocationAsync(includeDeleted: false);

                foreach (var s in students)
                {
                    // Safety: should already be non-null based on query, but keep it defensive.
                    if (s.StudyLatitude is null || s.StudyLongitude is null)
                        continue;

                    var pin = new Pin
                    {
                        Label = s.Name,
                        Address = s.StudyAddress,
                        Type = PinType.Place,
                        Location = new Location(s.StudyLatitude.Value, s.StudyLongitude.Value)
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
