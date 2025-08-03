using System;
using Microsoft.Maui.Controls;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    /// <summary>
    /// Code-behind for the StudentsListPage.
    /// Handles UI events and page-level logic.
    /// </summary>
    public partial class StudentsListPage : ContentPage
    {
        private readonly StudentsListViewModel viewModel;

        public StudentsListPage()
        {
            InitializeComponent();

            // Assign ViewModel to BindingContext
            viewModel = new StudentsListViewModel();
            BindingContext = viewModel;
        }

        /// <summary>
        /// Triggered when the user types in the search bar.
        /// This should be wired to a filtering method in the ViewModel.
        /// </summary>
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // TODO: Wire this up to the ViewModel's filtering logic
            Console.WriteLine($"Search query: {e.NewTextValue}");
        }

        /// <summary>
        /// Called when the floating '+' button is tapped.
        /// Navigates to the AddStudentPage to create a new student record.
        /// </summary>
        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddStudentPage());
        }
    }
}
