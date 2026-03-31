// ---------------------------------------------------------------------------------------------------------------------
// AddStudentViewModel.cs
//
// PURPOSE
// - Handles creation of a new Student record
// - Collects only STUDENT-level data (identity, contact, relationship state)
// - DOES NOT handle visit-specific data (that belongs in Visit)
//
// DESIGN RULES
// - Student = who the person is
// - Visit = what happened and when
// - Household = where they live (shared)
// - Keep this ViewModel focused and simple
//
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using System.Globalization;

namespace MinistryTracker.ViewModels
{
    public partial class AddStudentViewModel : ObservableObject
    {
        // Data access layer
        private readonly DataService _data;

        public AddStudentViewModel(DataService data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));

            // Command wiring
            SaveCommand = new AsyncRelayCommand(SaveStudentAsync);

            // Sensible defaults so UI isn't blank
            InitialContactType = InitialContactTypeValues.FirstOrDefault();
            FirstContactDate = DateTime.Now;
            Status = StudentStatus.Active;
            InterestLevel = InterestLevel.Promising;
        }

        // =====================================================================
        // 1) CORE STUDENT INPUT FIELDS (BOUND FROM UI)
        // =====================================================================

        /// <summary>
        /// Full name of the individual.
        /// Required (fallback handled at save).
        /// </summary>
        [ObservableProperty]
        public partial string Name { get; set; } = string.Empty;

        /// <summary>
        /// How the FIRST contact was made (historical, does not change).
        /// </summary>
        [ObservableProperty]
        public partial InitialContactType InitialContactType { get; set; }

        /// <summary>
        /// Date of first contact.
        /// </summary>
        [ObservableProperty]
        public partial DateTime FirstContactDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Phone number used for calling/texting.
        /// </summary>
        [ObservableProperty]
        public partial string? PhoneNumber { get; set; }

        /// <summary>
        /// Default/typical way we communicate with this person.
        /// (Memory aid only, not enforced.)
        /// </summary>
        [ObservableProperty]
        public partial ContactMethod? DefaultContactMethod { get; set; }

        /// <summary>
        /// Current ministry standing (progression).
        /// </summary>
        [ObservableProperty]
        public partial InterestLevel InterestLevel { get; set; } = InterestLevel.Promising;

        /// <summary>
        /// Overall relationship status.
        /// </summary>
        [ObservableProperty]
        public partial StudentStatus Status { get; set; } = StudentStatus.Active;

        /// <summary>
        /// Hard stop flag — should not be contacted again.
        /// </summary>
        [ObservableProperty]
        public partial bool IsDoNotCall { get; set; }

        /// <summary>
        /// Free-form notes about the individual.
        /// (Background, personality, important context.)
        /// </summary>
        [ObservableProperty]
        public partial string? Notes { get; set; }

        /// <summary>
        /// Optional link to a household (shared address/region).
        /// </summary>
        [ObservableProperty]
        public partial int? HouseholdId { get; set; }

        // =====================================================================
        // 2) PICKER SOURCES (ENUM → UI)
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
        // 3) COMMANDS
        // =====================================================================

        /// <summary>
        /// Saves the student to the database.
        /// </summary>
        public IAsyncRelayCommand SaveCommand { get; }

        // =====================================================================
        // 4) RESET (USED WHEN PAGE OPENS)
        // =====================================================================

        /// <summary>
        /// Resets form fields to a clean state.
        /// </summary>
        public void Reset()
        {
            Name = string.Empty;
            PhoneNumber = null;
            FirstContactDate = DateTime.Now;
            InitialContactType = InitialContactTypeValues.FirstOrDefault();
            DefaultContactMethod = null;
            InterestLevel = InterestLevel.Promising;
            Status = StudentStatus.Active;
            IsDoNotCall = false;
            Notes = null;
            HouseholdId = null;
        }

        // =====================================================================
        // 5) SAVE LOGIC
        // =====================================================================

        private async Task SaveStudentAsync()
        {
            // Fallback name if user leaves it blank
            if (string.IsNullOrWhiteSpace(Name))
            {
                var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                Name = uiLang == "es" ? "Desconocido" : "Unknown";
            }

            // Build Student model (IMPORTANT: only student-level data here)
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
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                HouseholdId = HouseholdId,
                IsDeleted = false
            };

            // Save to database
            var rows = await _data.AddStudentAsync(student);

            // Navigate back if successful
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