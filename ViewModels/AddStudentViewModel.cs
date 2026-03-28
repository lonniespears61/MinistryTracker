// AddStudentViewModel.cs

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Utilities;
using System.Globalization;
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

            // Device / app UI language (e.g., "en", "es")
            var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            PreferredLanguage = uiLang == "es" ? "Español" : "English";
        }

        // --------------------------------------------------------------------
        // Properties bound from XAML
        // --------------------------------------------------------------------

        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;                 // Required

        [ObservableProperty]
        public partial InitialCallType CallType { get; set; }                    // Required

        [ObservableProperty]
        public partial DateTime FirstContactDate { get; set; } = DateTime.Today; // Required

        [ObservableProperty]
        public partial string? PreferredLanguage { get; set; }

        [ObservableProperty]
        public partial string? Notes { get; set; } // Optional free text (no indexing)

        // Location fields (optional)
        [ObservableProperty]
        public partial double? StudyLatitude { get; set; }

        [ObservableProperty]
        public partial double? StudyLongitude { get; set; }

        // Picker ItemsSource
        public List<InitialCallType> CallTypeValues =>
            Enum.GetValues<InitialCallType>().ToList();

        // Commands
        public IAsyncRelayCommand SaveCommand { get; }
        public IAsyncRelayCommand UseCurrentLocationCommand { get; }

        // UI helpers (for your label)
        public bool IsLocationCaptured => StudyLatitude.HasValue && StudyLongitude.HasValue;

        [ObservableProperty]
        public partial bool IsLocating { get; set; }

        [ObservableProperty]
        public partial string? LocationStatus { get; set; }

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
            FirstContactDate = DateTime.Now;

            // Pick a sensible default (or replace with InitialCallType.HouseToHouse if preferred)
            CallType = CallTypeValues.FirstOrDefault();

            StudyLatitude = null;
            StudyLongitude = null;
            Notes = null;
        }

        private async Task UseCurrentLocationAsync()
        {
            IsLocating = true;
            LocationStatus = "Getting location...";

            try
            {
                // Ask for permission first
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
                var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                Name = uiLang == "es" ? "Desconocido" : "Unknown";
            }

            var student = new Student
            {
                Name = Name.Trim(),
                CallType = CallType,
                FirstContactDate = FirstContactDate,
                PreferredLanguage = PreferredLanguage,
                Status = StudentStatus.Active,
                IsDeleted = false,
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),

                // Persist GPS if captured (otherwise nulls)
                StudyLatitude = StudyLatitude,
                StudyLongitude = StudyLongitude
            };

            var rows = await _data.AddStudentAsync(student);

            // Use the current window page instead of obsolete Application.MainPage.
            var app = Application.Current;
            var page = app is not null && app.Windows.Count > 0
                ? app.Windows[0].Page
                : null;

            if (rows > 0)
            {
                if (page != null)
                    await page.Navigation.PopAsync();
            }
            else
            {
                if (page != null)
                    await page.DisplayAlert("Error", "Failed to add student. Please try again.", "OK");
            }
        }
    }
}