// ---------------------------------------------------------------------------------------------------------------------
// AddStudentViewModel.cs
//
// PURPOSE
// - ViewModel for AddStudentPage.
// - Collects required student fields.
// - Optionally captures current GPS location.
// - Saves a new Student record through DataService.
//
// NOTES
// - This project uses Shell navigation.
// - Do not use Application.Current.MainPage; it is obsolete in modern .NET MAUI.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
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

            CallType = CallTypeValues.FirstOrDefault();

            var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            PreferredLanguage = uiLang == "es" ? "Español" : "English";
        }

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private InitialCallType callType;

        [ObservableProperty]
        private DateTime firstContactDate = DateTime.Today;

        [ObservableProperty]
        private string? preferredLanguage;

        [ObservableProperty]
        private string? notes;

        [ObservableProperty]
        private double? studyLatitude;

        [ObservableProperty]
        private double? studyLongitude;

        [ObservableProperty]
        private bool isLocating;

        [ObservableProperty]
        private string? locationStatus;

        public List<InitialCallType> CallTypeValues =>
            Enum.GetValues(typeof(InitialCallType))
                .Cast<InitialCallType>()
                .ToList();

        public IAsyncRelayCommand SaveCommand { get; }

        public IAsyncRelayCommand UseCurrentLocationCommand { get; }

        public bool IsLocationCaptured =>
            StudyLatitude.HasValue && StudyLongitude.HasValue;

        public string LocationDisplay =>
            IsLocationCaptured
                ? $"Location captured ✓ ({StudyLatitude!.Value:F6}, {StudyLongitude!.Value:F6})"
                : string.Empty;

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

        public void Reset()
        {
            Name = string.Empty;

            var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            PreferredLanguage = uiLang == "es" ? "Español" : "English";

            FirstContactDate = DateTime.Today;
            CallType = CallTypeValues.FirstOrDefault();

            StudyLatitude = null;
            StudyLongitude = null;
            Notes = null;
            LocationStatus = null;
            IsLocating = false;
        }

        private async Task UseCurrentLocationAsync()
        {
            IsLocating = true;
            LocationStatus = "Getting location...";

            try
            {
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
                    LocationStatus = "Location failed: GPS unavailable.";
                    return;
                }

                StudyLatitude = loc.Latitude;
                StudyLongitude = loc.Longitude;

                LocationStatus = $"Captured ✓ ({StudyLatitude.Value:F6}, {StudyLongitude.Value:F6})";
            }
            catch (Exception ex)
            {
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
                StudyLatitude = StudyLatitude,
                StudyLongitude = StudyLongitude
            };

            var rows = await _data.AddStudentAsync(student);

            if (rows > 0)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            await Shell.Current.DisplayAlert(
                "Error",
                "Failed to add student. Please try again.",
                "OK");
        }
    }
}