using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Utilities;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class EditStudentViewModel : ObservableObject
    {
        private readonly DataService _data;

        // Keep reference so we preserve fields not edited on this page
        private Student? _loadedStudent;

        public EditStudentViewModel(DataService data) => _data = data;

        // --- Bindable fields (existing) ---
        [ObservableProperty] private int studentId;
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private DateTime firstContactDate = DateTime.Today;
        [ObservableProperty] private InitialCallType callType;

        [ObservableProperty] private bool isBusy;

        // --- Location fields (new) ---
        [ObservableProperty] private string? studyAddress;
        [ObservableProperty] private double? studyLatitude;
        [ObservableProperty] private double? studyLongitude;

        [ObservableProperty] private bool isLocating;
        [ObservableProperty] private string? locationStatus;

        public bool HasLocation => StudyLatitude.HasValue && StudyLongitude.HasValue;

        // -----------------------------
        // Load
        // -----------------------------
        public async Task LoadAsync(int id)
        {
            if (IsBusy) return;

            StudentId = id;

            try
            {
                IsBusy = true;

                var s = await _data.GetStudentByIdAsync(id);
                if (s is null)
                {
                    LocationStatus = "Student not found.";
                    return;
                }

                _loadedStudent = s;

                // Populate bindable properties
                Name = s.Name ?? string.Empty;
                FirstContactDate = s.FirstContactDate;
                CallType = s.CallType;

                StudyAddress = s.StudyAddress;
                StudyLatitude = s.StudyLatitude;
                StudyLongitude = s.StudyLongitude;

                LocationStatus = HasLocation
                    ? $"Location ✓ ({StudyLatitude:0.000000}, {StudyLongitude:0.000000})"
                    : "No location saved.";
            }
            catch (Exception ex)
            {
                LocationStatus = $"Load failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // -----------------------------
        // Save
        // -----------------------------
        [RelayCommand]
        public async Task<bool> SaveAsync()
        {
            if (IsBusy) return false;

            if (string.IsNullOrWhiteSpace(Name))
            {
                LocationStatus = "Name is required.";
                return false;
            }

            try
            {
                IsBusy = true;

                // Ensure we have the loaded student so we can preserve all other fields
                if (_loadedStudent is null || _loadedStudent.StudentId != StudentId)
                {
                    _loadedStudent = await _data.GetStudentByIdAsync(StudentId);
                }

                if (_loadedStudent is null)
                {
                    LocationStatus = "Student not found.";
                    return false;
                }

                // Update only what this page owns
                _loadedStudent.Name = Name.Trim();
                _loadedStudent.FirstContactDate = FirstContactDate;
                _loadedStudent.CallType = CallType;

                // Location fields
                _loadedStudent.StudyAddress = StudyAddress;
                _loadedStudent.StudyLatitude = StudyLatitude;
                _loadedStudent.StudyLongitude = StudyLongitude;

                await _data.UpdateStudentAsync(_loadedStudent);

                LocationStatus = "Saved ✓";
                return true;
            }
            catch (Exception ex)
            {
                LocationStatus = $"Save failed: {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // -----------------------------
        // Location commands
        // -----------------------------
        [RelayCommand]
        private async Task UseCurrentLocationAsync()
        {
            if (IsLocating) return;

            try
            {
                IsLocating = true;
                LocationStatus = "Capturing location…";

                if (!await LocationPermissionHelper.EnsureLocationPermissionAsync())
                {
                    LocationStatus = "Location permission denied.";
                    return;
                }

                var req = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var loc = await Geolocation.Default.GetLocationAsync(req);

                if (loc is null)
                {
                    LocationStatus = "Location unavailable.";
                    return;
                }

                StudyLatitude = loc.Latitude;
                StudyLongitude = loc.Longitude;

                LocationStatus = $"Location ✓ ({StudyLatitude:0.000000}, {StudyLongitude:0.000000})";

                // Optional: if address is empty, try to fill it automatically
                if (string.IsNullOrWhiteSpace(StudyAddress))
                    await ReverseGeocodeAsync();
            }
            catch
            {
                LocationStatus = "Location failed.";
            }
            finally
            {
                IsLocating = false;
            }
        }

        [RelayCommand]
        private async Task ReverseGeocodeAsync()
        {
            if (!HasLocation)
            {
                LocationStatus = "No coordinates to reverse-geocode.";
                return;
            }

            try
            {
                LocationStatus = "Looking up address…";

                var placemarks = await Geocoding.Default.GetPlacemarksAsync(
                    StudyLatitude!.Value, StudyLongitude!.Value);

                var p = placemarks?.FirstOrDefault();

                if (p is null)
                {
                    LocationStatus = "No address found for coordinates.";
                    return;
                }

                StudyAddress = string.Join(", ",
                    new[] { p.Thoroughfare, p.Locality, p.AdminArea, p.PostalCode }
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

                LocationStatus = "Address updated ✓";
            }
            catch
            {
                LocationStatus = "Reverse geocode failed.";
            }
        }

        [RelayCommand]
        private async Task GeocodeFromAddressAsync()
        {
            if (string.IsNullOrWhiteSpace(StudyAddress))
            {
                LocationStatus = "No address to geocode.";
                return;
            }

            try
            {
                LocationStatus = "Geocoding address…";

                var locations = await Geocoding.Default.GetLocationsAsync(StudyAddress);
                var loc = locations?.FirstOrDefault();

                if (loc is null)
                {
                    LocationStatus = "Could not find coordinates for address.";
                    return;
                }

                StudyLatitude = loc.Latitude;
                StudyLongitude = loc.Longitude;

                LocationStatus = $"Coordinates ✓ ({StudyLatitude:0.000000}, {StudyLongitude:0.000000})";
            }
            catch
            {
                LocationStatus = "Geocode failed.";
            }
        }

        // Keep HasLocation reactive for XAML bindings (button enable/disable)
        partial void OnStudyLatitudeChanged(double? value) =>
            OnPropertyChanged(nameof(HasLocation));

        partial void OnStudyLongitudeChanged(double? value) =>
            OnPropertyChanged(nameof(HasLocation));
    }
}
