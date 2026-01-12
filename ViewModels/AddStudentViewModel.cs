// AddStudentViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;           // Application, etc.
using Microsoft.Maui.Devices.Sensors;    // Geolocation, Location
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class AddStudentViewModel : ObservableObject
    {
        private readonly DataService _data;

        public AddStudentViewModel(DataService data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));

            SaveCommand = new AsyncRelayCommand(SaveStudentAsync);
            UseCurrentLocationCommand = new AsyncRelayCommand(UseCurrentLocationAsync);

            // Optional: set a reasonable default so the Picker isn't blank
            CallType = CallTypeValues.FirstOrDefault();
        }

        // --------------------------------------------------------------------
        // Properties bound from XAML
        // --------------------------------------------------------------------

        [ObservableProperty] private string name = string.Empty;                // Required
        [ObservableProperty] private InitialCallType callType;                  // Required
        [ObservableProperty] private DateTime firstContactDate = DateTime.Today; // Required
        [ObservableProperty] private string? preferredLanguage;                 // Optional

        // Location fields (optional)
        [ObservableProperty] private double? studyLatitude;
        [ObservableProperty] private double? studyLongitude;

        // Picker ItemsSource
        public List<InitialCallType> CallTypeValues =>
            Enum.GetValues(typeof(InitialCallType)).Cast<InitialCallType>().ToList();

        // Commands
        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand UseCurrentLocationCommand { get; }

        // UI helpers (for your label)
        public bool IsLocationCaptured => StudyLatitude.HasValue && StudyLongitude.HasValue;
        [ObservableProperty] private bool isLocating;
        [ObservableProperty] private string? locationStatus;

        public string LocationDisplay =>
            IsLocationCaptured
                ? $"Location captured ✓ ({StudyLatitude!.Value:F6}, {StudyLongitude!.Value:F6})"
                : string.Empty;

        // Keep computed properties updated when lat/lon changes
        partial void OnStudyLatitudeChanged(double? value)
        {
            OnPropertyChanged(nameof(IsLocationCaptured));
            OnPropertyChanged(nameof(LocationDisplay));
        }

        partial void OnStudyLongitudeChanged(double? value)
        {
            OnPropertyChanged(nameof(IsLocationCaptured));
            OnPropertyChanged(nameof(LocationDisplay));
        }

        // Called by page OnAppearing() to ensure clean slate
        public void Reset()
        {
            Name = string.Empty;
            PreferredLanguage = null;
            FirstContactDate = DateTime.Today;

            // Pick a sensible default (or replace with InitialCallType.HouseToHouse if preferred)
            CallType = CallTypeValues.FirstOrDefault();

            StudyLatitude = null;
            StudyLongitude = null;
        }

        private async Task UseCurrentLocationAsync()
        {
            IsLocating = true;
            LocationStatus = "Getting location...";

            try
            {
                // ✅ Ask for permission first
                var granted = await LocationPermissionHelper.EnsureLocationPermissionAsync();
                if (!granted)
                {
                    LocationStatus = "Location failed: permission not granted.";
                    return;
                }

                var request = new GeolocationRequest(
                    GeolocationAccuracy.Medium,
                    TimeSpan.FromSeconds(10));

                var loc = await Geolocation.Default.GetLocationAsync(request);

                if (loc is null)
                {
                    LocationStatus = "Location failed: GPS unavailable (emulator location not set?).";
                    return;
                }

                StudyLatitude = loc.Latitude;
                StudyLongitude = loc.Longitude;

                LocationStatus = $"Captured ✓ ({StudyLatitude.Value:F6}, {StudyLongitude.Value:F6})";
            }
            catch (Exception ex)
            {
                // Helpful for now; we can soften later
                LocationStatus = $"Location failed: {ex.Message}";
            }
            finally
            {
                IsLocating = false;
            }
        }


        private async Task SaveStudentAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                await Application.Current.MainPage.DisplayAlert("Missing Info", "Name is required.", "OK");
                return;
            }

            var student = new Student
            {
                Name = Name.Trim(),
                CallType = CallType,
                FirstContactDate = FirstContactDate,
                PreferredLanguage = PreferredLanguage,
                Status = StudentStatus.Active,
                IsDeleted = false,

                // Persist GPS if captured (otherwise nulls)
                StudyLatitude = StudyLatitude,
                StudyLongitude = StudyLongitude
            };

            var rows = await _data.AddStudentAsync(student);

            if (rows > 0)
            {
                await Application.Current.MainPage.Navigation.PopAsync();
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to add student. Please try again.", "OK");
            }
        }
    }
}
