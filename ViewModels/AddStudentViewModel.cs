// ---------------------------------------------------------------------------------------------------------------------
// AddStudentViewModel.cs
//
// PURPOSE
// - Handles creation of a new Student record.
// - Keeps Add Student focused on first-entry person data only.
// - Leaves Do Not Call, deletion, and broader relationship management for later workflows.
//
// WHY THIS VERSION CHANGED
// - Student now has real address/location fields, so address is no longer folded into Notes.
// - Add Student only collects Tier 1 and Tier 2 fields agreed in review.
// - GPS capture stores coordinates now, but address replacement stays user-confirmed.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices.Sensors;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class AddStudentViewModel : ObservableObject
    {
        private readonly DataService _data;

        // Captured GPS state for this Add flow.
        private double? _capturedLatitude;
        private double? _capturedLongitude;
        private GeocodeStatus _capturedGeocodeStatus = GeocodeStatus.None;

        // Allow readable identifiers and common punctuation.
        // Reject obvious symbol soup / bad input.
        private static readonly Regex ValidNamePattern = new(
            @"^[\p{L}\p{N}\s'\-.,/&]+$",
            RegexOptions.Compiled);

        public AddStudentViewModel(DataService data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            Reset();
        }

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when a student is saved successfully.
        /// The View can respond by prompting for next steps such as scheduling a visit.
        /// </summary>
        public event Action? SaveCompleted;

        /// <summary>
        /// Fired when save fails.
        /// The View should respond by showing an alert.
        /// </summary>
        public event Action<string>? SaveFailed;

        /// <summary>
        /// Fired when GPS capture produced a suggested address that differs from
        /// the current typed address. The View can prompt the user to confirm
        /// whether the suggested address should replace the typed one.
        /// </summary>
        public event Action<string?, string>? AddressReplacementSuggested;

        // =====================================================================
        // FORM FIELDS
        // =====================================================================

        /// <summary>
        /// User-facing identifier for the person.
        /// This may be a real name or a meaningful placeholder entered by the user.
        /// If left blank, save falls back to "Name Unknown".
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// User-facing address field for Add Student.
        /// This maps to Student.PrimaryAddress when saved.
        /// The property name is kept as Address here to minimize ripple into the page.
        /// </summary>
        [ObservableProperty]
        private string? _address;

        /// <summary>
        /// True when the address is the student's home address.
        /// </summary>
        [ObservableProperty]
        private bool _isHomeAddress;

        /// <summary>
        /// Date of first contact.
        /// This is intended to be the historical first contact date, not app-entry date.
        /// </summary>
        [ObservableProperty]
        private DateTime _firstContactDate = DateTime.Now;

        /// <summary>
        /// How the first contact was made.
        /// This is historical and remains editable on Add for accurate backfill.
        /// </summary>
        [ObservableProperty]
        private InitialContactType _initialContactType;

        /// <summary>
        /// Optional phone number or contact number.
        /// </summary>
        [ObservableProperty]
        private string? _phoneNumber;

        /// <summary>
        /// Optional demographic field.
        /// </summary>
        [ObservableProperty]
        private Gender? _selectedGender;

        /// <summary>
        /// Optional demographic field.
        /// </summary>
        [ObservableProperty]
        private int? _studentAge;

        /// <summary>
        /// Optional notes captured during entry.
        /// </summary>
        [ObservableProperty]
        private string? _notes;

        /// <summary>
        /// True while location capture is in progress.
        /// </summary>
        [ObservableProperty]
        private bool _isLocating;

        /// <summary>
        /// Prevents duplicate save attempts and supports loading UI.
        /// </summary>
        [ObservableProperty]
        private bool _isBusy;

        /// <summary>
        /// Holds a GPS-derived address suggestion when it differs from the typed address.
        /// The View can use this to prompt the user before replacing Address.
        /// </summary>
        [ObservableProperty]
        private string? _gpsSuggestedAddress;

        /// <summary>
        /// Exposes the last saved student ID when sqlite populates it on insert.
        /// This gives the View a clean way to branch into scheduling later.
        /// </summary>
        [ObservableProperty]
        private int? _savedStudentId;

        // =====================================================================
        // PICKER SOURCES
        // =====================================================================

        public List<InitialContactType> InitialContactTypeValues =>
            Enum.GetValues<InitialContactType>().ToList();

        public List<Gender> GenderValues =>
            Enum.GetValues<Gender>().ToList();

        // =====================================================================
        // RESET
        // =====================================================================

        /// <summary>
        /// Returns the form to a clean Add Student state.
        /// Only Add-stage fields are reset here.
        /// </summary>
        public void Reset()
        {
            Name = string.Empty;
            Address = null;
            IsHomeAddress = false;
            PhoneNumber = null;
            FirstContactDate = DateTime.Now;
            InitialContactType = InitialContactTypeValues.FirstOrDefault();
            SelectedGender = null;
            StudentAge = null;
            Notes = null;

            IsLocating = false;
            IsBusy = false;

            GpsSuggestedAddress = null;
            SavedStudentId = null;

            _capturedLatitude = null;
            _capturedLongitude = null;
            _capturedGeocodeStatus = GeocodeStatus.None;
        }

        // =====================================================================
        // LOCATION
        // =====================================================================

        /// <summary>
        /// Captures the current device location.
        /// GPS coordinates are treated as the best return point when captured on site.
        /// If reverse geocoding produces a usable address, the View is asked to confirm
        /// before replacing any typed address that already exists.
        /// </summary>
        [RelayCommand]
        private async Task UseCurrentLocationAsync()
        {
            if (IsLocating)
                return;

            try
            {
                IsLocating = true;

                var granted = await LocationPermissionHelper
                    .EnsureLocationPermissionAsync()
                    .ConfigureAwait(false);

                if (!granted)
                    return;

                var request = new GeolocationRequest(
                    GeolocationAccuracy.Medium,
                    TimeSpan.FromSeconds(10));

                var location = await Geolocation.Default
                    .GetLocationAsync(request)
                    .ConfigureAwait(false);

                if (location is null)
                    return;

                _capturedLatitude = location.Latitude;
                _capturedLongitude = location.Longitude;
                _capturedGeocodeStatus = ResolveGeocodeStatusOrDefault(
                    fallback: GeocodeStatus.None,
                    preferredNames: new[]
                    {
                        "GpsCaptured",
                        "GPSCaptured",
                        "CoordinatesCaptured",
                        "Captured",
                        "Resolved",
                        "Success"
                    });

                string? suggestedAddress = null;

                try
                {
                    var placemarks = await Geocoding.Default
                        .GetPlacemarksAsync(location.Latitude, location.Longitude)
                        .ConfigureAwait(false);

                    var place = placemarks?.FirstOrDefault();

                    if (place is not null)
                    {
                        suggestedAddress = BuildAddressFromPlacemark(place);

                        if (!string.IsNullOrWhiteSpace(suggestedAddress))
                        {
                            _capturedGeocodeStatus = ResolveGeocodeStatusOrDefault(
                                fallback: _capturedGeocodeStatus,
                                preferredNames: new[]
                                {
                                    "Resolved",
                                    "Success",
                                    "GpsCaptured",
                                    "GPSCaptured",
                                    "CoordinatesCaptured",
                                    "Captured"
                                });
                        }
                    }
                }
                catch
                {
                    // Keep captured coordinates even if reverse geocoding fails.
                }

                if (string.IsNullOrWhiteSpace(suggestedAddress))
                {
                    GpsSuggestedAddress = null;
                    return;
                }

                if (string.IsNullOrWhiteSpace(Address))
                {
                    Address = suggestedAddress;
                    GpsSuggestedAddress = null;
                    return;
                }

                if (AddressesMatch(Address, suggestedAddress))
                {
                    GpsSuggestedAddress = null;
                    return;
                }

                GpsSuggestedAddress = suggestedAddress;
                AddressReplacementSuggested?.Invoke(Address, suggestedAddress);
            }
            catch (FeatureNotSupportedException)
            {
                SaveFailed?.Invoke("Location is not supported on this device.");
            }
            catch (PermissionException)
            {
                SaveFailed?.Invoke("Location permission was denied.");
            }
            catch (Exception ex)
            {
                SaveFailed?.Invoke(ex.Message);
            }
            finally
            {
                IsLocating = false;
            }
        }

        /// <summary>
        /// Called by the View after prompting the user.
        /// Replaces the typed address with the GPS-derived suggested address.
        /// </summary>
        public void ApplySuggestedGpsAddress()
        {
            if (!string.IsNullOrWhiteSpace(GpsSuggestedAddress))
            {
                Address = GpsSuggestedAddress;
            }

            GpsSuggestedAddress = null;
        }

        /// <summary>
        /// Called by the View after prompting the user.
        /// Keeps the typed address and clears the pending suggestion.
        /// </summary>
        public void KeepTypedAddress()
        {
            GpsSuggestedAddress = null;
        }

        // =====================================================================
        // SAVE
        // =====================================================================

        /// <summary>
        /// Saves the new Student.
        /// Only Add-stage fields are mapped here.
        /// Relationship admin fields remain in later workflows.
        /// </summary>
        [RelayCommand]
        private async Task SaveStudentAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                var cleanedName = NormalizeNameOrFallback(Name);

                if (!IsValidIdentifier(cleanedName))
                {
                    SaveFailed?.Invoke("Name contains unsupported characters.");
                    return;
                }

                if (StudentAge is < 0)
                {
                    SaveFailed?.Invoke("Age cannot be negative.");
                    return;
                }

                var student = new Student
                {
                    Name = cleanedName,
                    InitialContactType = InitialContactType,
                    FirstContactDate = FirstContactDate,
                    PhoneNumber = NormalizeOptionalText(PhoneNumber),
                    PrimaryAddress = NormalizeOptionalText(Address),
                    IsHomeAddress = IsHomeAddress,
                    PrimaryLatitude = _capturedLatitude,
                    PrimaryLongitude = _capturedLongitude,
                    PrimaryGeocodeStatus = _capturedGeocodeStatus,
                    Gender = SelectedGender,
                    Age = StudentAge,
                    Notes = NormalizeOptionalText(Notes)
                };

                var rows = await _data.AddStudentAsync(student).ConfigureAwait(false);

                if (rows > 0)
                {
                    SavedStudentId = student.StudentId;
                    SaveCompleted?.Invoke();
                }
                else
                {
                    SaveFailed?.Invoke("Failed to add student. Please try again.");
                }
            }
            catch (Exception ex)
            {
                SaveFailed?.Invoke(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static string NormalizeNameOrFallback(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Name Unknown";

            var cleaned = CollapseWhitespace(value.Trim());
            return string.IsNullOrWhiteSpace(cleaned) ? "Name Unknown" : cleaned;
        }

        private static string? NormalizeOptionalText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var cleaned = CollapseWhitespace(value.Trim());
            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        private static string CollapseWhitespace(string value)
        {
            return Regex.Replace(value, @"\s+", " ").Trim();
        }

        private static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return ValidNamePattern.IsMatch(value);
        }

        private static bool AddressesMatch(string currentAddress, string suggestedAddress)
        {
            var left = NormalizeAddressForCompare(currentAddress);
            var right = NormalizeAddressForCompare(suggestedAddress);

            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeAddressForCompare(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var cleaned = value.Trim().ToUpperInvariant();

            cleaned = cleaned.Replace(".", string.Empty)
                             .Replace(",", string.Empty);

            cleaned = Regex.Replace(cleaned, @"\s+", " ");

            return cleaned;
        }

        private static string? BuildAddressFromPlacemark(Placemark place)
        {
            var line1Parts = new[]
            {
                place.SubThoroughfare,
                place.Thoroughfare
            }
            .Where(p => !string.IsNullOrWhiteSpace(p));

            var line2Parts = new[]
            {
                place.Locality,
                place.AdminArea,
                place.PostalCode
            }
            .Where(p => !string.IsNullOrWhiteSpace(p));

            var line1 = string.Join(" ", line1Parts);
            var line2 = string.Join(", ", line2Parts);

            if (!string.IsNullOrWhiteSpace(line1) && !string.IsNullOrWhiteSpace(line2))
                return $"{line1}, {line2}";

            if (!string.IsNullOrWhiteSpace(line1))
                return line1;

            if (!string.IsNullOrWhiteSpace(line2))
                return line2;

            return null;
        }

        private static GeocodeStatus ResolveGeocodeStatusOrDefault(
            GeocodeStatus fallback,
            IEnumerable<string> preferredNames)
        {
            foreach (var name in preferredNames)
            {
                if (Enum.TryParse<GeocodeStatus>(name, ignoreCase: true, out var parsed))
                    return parsed;
            }

            return fallback;
        }
    }
}  
