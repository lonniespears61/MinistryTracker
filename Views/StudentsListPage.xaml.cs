using Microsoft.Maui.Controls; // Core Maui UI types
using MinistryTracker.Models;  // For Student model
using MinistryTracker.ViewModels; // For StudentViewModel
using System;
using System.Diagnostics;

namespace MinistryTracker.Views
{
    /// <summary>
    /// Code-behind for the StudentsListPage.xaml.
    /// Handles user interaction events like tapping and navigation.
    /// </summary>
    public partial class StudentsListPage : ContentPage
    {
        // Local reference to the ViewModel
        private readonly StudentsListViewModel viewModel;

        public StudentsListPage()
        {
            InitializeComponent();

            // Create an instance of the ViewModel
            viewModel = new StudentsListViewModel();

            // Set the ViewModel as the data context for data binding
            BindingContext = viewModel;
        }

        /// <summary>
        /// Triggered when the search bar text is changed.
        /// Currently a stub for filtering logic.
        /// </summary>
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // Eventually this should call a ViewModel filter method
            Console.WriteLine($"Search query: {e.NewTextValue}");
        }

        /// <summary>
        /// Triggered when the "+" button is tapped.
        /// Navigates to the AddStudentPage to create a new record.
        /// </summary>
        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddStudentPage());
        }

        /// <summary>
        /// Triggered when a student card (Border) is tapped.
        /// Safely extracts the Student model and navigates to the profile page.
        /// </summary>
        private async void OnStudentTapped(object sender, EventArgs e)
        {
            // Log what was tapped
            Debug.WriteLine($"Tapped on: {sender}");

            // Ensure the sender is a Border and has a StudentViewModel bound to it
            if (sender is Border border && border.BindingContext is StudentViewModel studentVM)
            {
                // Extract the underlying Student model
                var tappedStudent = studentVM.Model;

                Debug.WriteLine($"Navigating to profile for student: {tappedStudent.Name}");

                // ✅ Pass the Student directly to the StudentProfilePage
                await Navigation.PushAsync(new StudentProfilePage(tappedStudent));
            }
            else
            {
                // Fallback debug message for unexpected binding
                Debug.WriteLine("Tapped item was not a StudentViewModel.");
            }
        }
    }
}
