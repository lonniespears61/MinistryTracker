// ---------------------------------------------------------------------------------------------------------------------
// AddStudentViewModel.cs
//
// PURPOSE
// - Handles creation of a new Student record.
// - Collects only STUDENT-level data (identity, contact, relationship state).
// - Address captured here is first-pass reference data captured at time of entry.
//
// DESIGN RULES
// - Student = person / relationship state
// - Visit = interaction history / meeting details
// - ViewModel must not call Application, Page, Navigation, or Shell directly
// - View handles navigation and alerts
//
// WHY EVENTS?
// - This is a simple 1:1 page-to-viewmodel relationship.
// - Plain C# events are easy to follow and lower overhead than a message bus here.
//
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
            Reset();
        }

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when a student is saved successfully.
        /// The View should respond by navigating back.
        /// </summary>
        public event Action? SaveCompleted;

        /// <summary>
        /// Fired when save fails.
        /// The View should respond by showing an alert.
        /// </summary>
        public event Action<string>? SaveFailed;

        // =====================================================================
        // FORM FIELDS
        // =====================================================================

        /// <summary>
        /// Full name of the individual.
        /// Required, but fallback is applied during save if left blank.
        /// </summary>
        [ObservableProperty]
        private string name = string.Empty;

        /// <summary>
        /// Optional address captured during first entry.
        /// This is useful memory aid information and may later help establish
        /// household/home data.
        /// </summary>
        [ObservableProperty]
        private string? address;

        /// <summary>
        /// True while geolocation is in progress.
        /// </summary>
        [ObservableProperty]
        private bool isLocating;

        /// <summary>
        /// How the first contact was made.
        /// Historical value; does not change later.
        /// </summary>
        [ObservableProperty]
        private InitialContactType initialContactType;

        /// <summary>
        /// Date of first contact.
        /// </summary>
        [ObservableProperty]
        private DateTime firstContactDate = DateTime.Now;

        /// <summary>
        /// Phone number used for calling/texting.
        /// </summary>
        [ObservableProperty]
        private string? phoneNumber;

        /// <summary>
        /// Default or usual contact method.
        /// Memory aid only; not enforced.
        /// </summary>
        [ObservableProperty]
        private ContactMethod? defaultContactMethod;

        /// <summary>
        /// Current ministry standing/progression.
        /// </summary>
        [ObservableProperty]
        private InterestLevel interestLevel = InterestLevel.Promising;

        /// <summary>
        /// Overall relationship status.
        /// </summary>
        [ObservableProperty]
        private StudentStatus status = StudentStatus.Active;

        /// <summary>
        /// Hard-stop flag — should not be contacted again.
        /// </summary>
        [ObservableProperty]
        private bool isDoNotCall;

        /// <summary>
        /// Free-form notes about the individual.
        /// </summary>
        [ObservableProperty]
        private string? notes;

        /// <summary>
        /// Optional link to a shared household record.
        /// </summary>
        [ObservableProperty]
        private int? householdId;

        /// <summary>
        /// Prevents duplicate save attempts and helps drive loading UI.
        /// </summary>
        [ObservableProperty]
        private bool isBusy;

        // =====================================================================
        // PICKER SOURCES
        // =====================================================================

        public List<InitialContactType> InitialContactTypeValues =>
            Enum.GetValues<InitialContactType>().ToList();

        public List<ContactMethod> ContactMethodValues =>
            Enum.GetValues<ContactMethod>().ToList();

        public List<InterestLevel> InterestLevelValues =>
            Enum.GetValues<InterestLevel>().ToList();

        public List<StudentStatus> StudentStatusValues =>
            Enum.GetValues<StudentStatus>().ToList();

        // =====================================================================
        // RESET
        // =====================================================================

        /// <summary>
        /// Reset form fields to a clean starting state.
        /// Called when the page appears.
        /// </summary>
        public void Reset()
        {
            Name = string.Empty;
            Address = null;
            PhoneNumber = null;
            FirstContactDate = DateTime.Now;
            InitialContactType = InitialContactTypeValues.FirstOrDefault();
            DefaultContactMethod = null;
            InterestLevel = InterestLevel.Promising;
            Status = StudentStatus.Active;
            IsDoNotCall = false;
            Notes = null;
            HouseholdId = null;
            IsLocating = false;
            IsBusy = false;
        }

        // =====================================================================
        // LOCATION
        // =====================================================================

        /// <summary>
        /// Capture current device location and try to turn it into a readable address.
        /// Falls back to coordinates if reverse geocoding is unavailable.
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

                try
                {
                    var placemarks = await Geocoding.Default
                        .GetPlacemarksAsync(location.Latitude, location.Longitude)
                        .ConfigureAwait(false);

                    var place = placemarks?.FirstOrDefault();

                    if (place is not null)
                    {
                        var parts = new[]
                        {
                            place.SubThoroughfare,
                            place.Thoroughfare,
                            place.Locality
                        }
                        .Where(p => !string.IsNullOrWhiteSpace(p));

                        Address = string.Join(" ", parts);
                    }
                    else
                    {
                        Address = $"{location.Latitude:F5}, {location.Longitude:F5}";
                    }
                }
                catch
                {
                    Address = $"{location.Latitude:F5}, {location.Longitude:F5}";
                }
            }
            catch (FeatureNotSupportedException)
            {
                // Device does not support this feature.
            }
            catch (PermissionException)
            {
                // Permission issue occurred during request.
            }
            catch
            {
                // Silent failure for now; we can add richer UX later if needed.
            }
            finally
            {
                IsLocating = false;
            }
        }

        // =====================================================================
        // SAVE
        // =====================================================================

        /// <summary>
        /// Save the new student record.
        /// Success/failure is signaled to the View using events.
        /// </summary>
        [RelayCommand]
        private async Task SaveStudentAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                if (string.IsNullOrWhiteSpace(Name))
                {
                    var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                    Name = uiLang == "es" ? "Desconocido" : "Unknown";
                }

                var studentNotes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();

                // If an address was captured at first entry, fold it into notes for now.
                // Student does not currently own dedicated address fields.
                if (!string.IsNullOrWhiteSpace(Address))
                {
                    var addressLine = $"Address: {Address.Trim()}";
                    studentNotes = string.IsNullOrWhiteSpace(studentNotes)
                        ? addressLine
                        : $"{addressLine}{Environment.NewLine}{studentNotes}";
                }

                var student = new Student
                {
                    Name = Name.Trim(),
                    InitialContactType = InitialContactType,
                    FirstContactDate = FirstContactDate,
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                    DefaultContactMethod = DefaultContactMethod,
                    InterestLevel = InterestLevel,
                    Status = Status,
                    IsDoNotCall = IsDoNotCall,
                    Notes = studentNotes,
                    HouseholdId = HouseholdId,
                    IsDeleted = false
                };

                var rows = await _data.AddStudentAsync(student).ConfigureAwait(false);

                if (rows > 0)
                {
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
    }
}