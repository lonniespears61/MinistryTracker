// StudentsListPage.xaml.cs
// Purpose: Hosts the searchable list of students.
// Notes:
// - ViewModel and IServiceProvider are injected via DI.
// - OnAppearing loads/refeshes the list (guarded to avoid overlapping calls).
// - SelectionChanged navigates to Edit; FAB navigates to Add.
// - Includes defensive null checks and simple error surfacing.

using System;
using System.Linq;                          // For FirstOrDefault()
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;              // ContentPage, SelectionChangedEventArgs
using MinistryTracker.Models;               // Student (model bound in the list)
using MinistryTracker.ViewModels;           // StudentsListViewModel

namespace MinistryTracker.Views
{
    public partial class StudentsListPage : ContentPage
    {
        // Injected ViewModel (BindingContext)
        private readonly StudentsListViewModel _vm;

        // DI container for resolving other pages
        private readonly IServiceProvider _services;

        // Prevent overlapping loads when OnAppearing fires multiple times quickly (Android)
        private bool _isLoading;

        public StudentsListPage(StudentsListViewModel vm, IServiceProvider services)
        {
            InitializeComponent();

            BindingContext = _vm = vm;
            _services = services;
        }

        /// <summary>
        /// Refresh data each time the page appears.
        /// Guarded so we don't run multiple loads concurrently.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_isLoading) return;
            _isLoading = true;

            try
            {
                await _vm.LoadAsync();

                // Clear selection (so the same row can be tapped again later)
                if (StudentsCollection != null)
                    StudentsCollection.SelectedItem = null;
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[StudentsListPage] Load failed: {ex}");
#endif
                await DisplayAlert("Error", "Couldn't load students. Please try again.", "OK");
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Floating action button → navigate to Add Student.
        /// </summary>
        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            try
            {
                var addPage = _services.GetRequiredService<AddStudentPage>();
                await Navigation.PushAsync(addPage);
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[StudentsListPage] Navigate to Add failed: {ex}");
#endif
                await DisplayAlert("Error", "Couldn't open Add Student.", "OK");
            }
        }

        /// <summary>
        /// When a student is selected, navigate to Edit page.
        /// </summary>
        private async void OnStudentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Ignore deselection or empty selection events
                var selected = e.CurrentSelection?.FirstOrDefault();
                if (selected is not Student student) return;

                var editPage = _services.GetRequiredService<EditStudentPage>();
                editPage.Init(student); // Pass the selected Student model
                await Navigation.PushAsync(editPage);
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[StudentsListPage] Navigate to Edit failed: {ex}");
#endif
                await DisplayAlert("Error", "Couldn't open Student details.", "OK");
            }
            finally
            {
                if (StudentsCollection != null)
                    StudentsCollection.SelectedItem = null;
            }
        }
    }
}
