using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MinistryTracker.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel for the AddStudentPage.
    /// Uses the MVVM Toolkit for cleaner and automatic property change notification.
    /// </summary>
    public partial class AddStudentViewModel : ObservableObject
    {
        // ----------------- Bound Properties -----------------
        // The [ObservableProperty] attribute automatically generates:
        // - a private field _name
        // - a public property 'Name' with get/set
        // - INotifyPropertyChanged calls for binding updates

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private Gender? selectedGender;

        [ObservableProperty]
        private string? preferredLanguage;

        [ObservableProperty]
        private int? age;

        [ObservableProperty]
        private ContactMethod? selectedContactMethod;

        [ObservableProperty]
        private InterestLevel selectedInterestLevel = InterestLevel.Potential;

        [ObservableProperty]
        private InitialCallType callType;

        [ObservableProperty]
        private DateTime firstContactDate = DateTime.Today;

        [ObservableProperty]
        private StudyLocationType? selectedStudyLocationType;

        // Pre-populated lists for Pickers (bind to these in XAML)
        public List<Gender> GenderValues => Enum.GetValues(typeof(Gender)).Cast<Gender>().ToList();
        public List<ContactMethod> ContactMethodValues => Enum.GetValues(typeof(ContactMethod)).Cast<ContactMethod>().ToList();
        public List<InterestLevel> InterestLevelValues => Enum.GetValues(typeof(InterestLevel)).Cast<InterestLevel>().ToList();
        public List<StudyLocationType> StudyLocationTypeValues => Enum.GetValues(typeof(StudyLocationType)).Cast<StudyLocationType>().ToList();

        // ----------------- Command -----------------

        /// <summary>
        /// Command for saving the student entry.
        /// Binds to Save button in the UI.
        /// </summary>
        public IRelayCommand SaveCommand { get; }

        public AddStudentViewModel()
        {
            // Hook up the Save command to the method
            SaveCommand = new AsyncRelayCommand(SaveStudentAsync);
        }

        /// <summary>
        /// Saves the new student into the database and displays confirmation.
        /// </summary>
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
                Gender = SelectedGender,
                PreferredLanguage = PreferredLanguage,
                Age = Age,
                ContactMethod = SelectedContactMethod,
                InterestLevel = SelectedInterestLevel,
                StudyLocationType = SelectedStudyLocationType ?? StudyLocationType.Home,
                Status = StudentStatus.Active,
                IsDeleted = false
            };

            int rowsAffected = await App.Database.AddStudentAsync(student);

            if (rowsAffected > 0)
            {
                await Toast.Make("Student added successfully!", ToastDuration.Short).Show();

                string action = await Application.Current.MainPage.DisplayActionSheet(
                    "What would you like to do next?",
                    "Cancel",
                    null,
                    "Add Another",
                    "Return to Student List"
                );

                if (action == "Add Another")
                {
                    // Reset only essential fields
                    Name = string.Empty;
                    CallType = InitialCallType.HouseToHouse;
                    FirstContactDate = DateTime.Today;
                }
                else if (action == "Return to Student List")
                {
                    await Application.Current.MainPage.Navigation.PopAsync();
                }
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Failed to add student. Please try again.", "OK");
            }
        }
    }
}
