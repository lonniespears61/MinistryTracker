// StudentsListPage.xaml.cs (DROP-IN for Shell conversion)
//
// TAP BEHAVIOR (UPDATED):
// - Tap student row = "Next Visit"
//     If a future visit exists -> open UpdateVisitPage (visitId)
//     Else -> open AddVisitPage (studentId)
//
// WHY:
// - Your UX rule: one future visit per student is the norm.
// - Prevents creating duplicate scheduled visits.
// - Keeps DB calls out of the View (the VM preloads NextFutureVisitId).
//
// IMPORTANT PREREQS (one-time, in AppShell):
// - Routing.RegisterRoute(nameof(StudentProfilePage), typeof(StudentProfilePage));
// - Routing.RegisterRoute(nameof(EditStudentPage), typeof(EditStudentPage));
// - Routing.RegisterRoute(nameof(AddVisitPage), typeof(AddVisitPage));
// - Routing.RegisterRoute(nameof(UpdateVisitPage), typeof(UpdateVisitPage));  <-- NEW for tap behavior
// - Routing.RegisterRoute(nameof(AddStudentPage), typeof(AddStudentPage));

using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;
using System;
using System.Linq;

namespace MinistryTracker.Views
{
    public partial class StudentsListPage : ContentPage
    {
        public StudentsListPage(StudentsListViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        // ------------------------------------------------------------
        // SHELL NAVIGATION HELPERS
        // ------------------------------------------------------------

        /// <summary>
        /// Optional: still available if you later want a "Details" action somewhere.
        /// Not used by row tap anymore.
        /// </summary>
        private static async System.Threading.Tasks.Task GoToStudentProfileAsync(int studentId)
        {
            await Shell.Current.GoToAsync($"{nameof(StudentProfilePage)}?studentId={studentId}");
        }

        private static async System.Threading.Tasks.Task GoToEditStudentAsync(int studentId)
        {
            await Shell.Current.GoToAsync($"{nameof(EditStudentPage)}?studentId={studentId}");
        }

        private static async System.Threading.Tasks.Task GoToAddVisitAsync(int studentId)
        {
            await Shell.Current.GoToAsync($"{nameof(AddVisitPage)}?studentId={studentId}");
        }

        /// <summary>
        /// NEW: Navigate to UpdateVisitPage when a future visit already exists.
        /// </summary>
        private static async System.Threading.Tasks.Task GoToUpdateVisitAsync(int visitId)
        {
            await Shell.Current.GoToAsync($"{nameof(UpdateVisitPage)}?visitId={visitId}");
        }

        private static async System.Threading.Tasks.Task GoToAddStudentAsync()
        {
            await Shell.Current.GoToAsync(nameof(AddStudentPage));
        }

        // ------------------------------------------------------------
        // COLLECTIONVIEW SELECTION (TAP)
        // ------------------------------------------------------------
        // XAML: SelectionChanged="OnStudentSelectionChanged"
        private async void OnStudentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Items are StudentViewModel in your CollectionView template.
                var svm = e.CurrentSelection?.FirstOrDefault() as StudentViewModel;
                if (svm?.Model is null) return;

                // Clear selection ASAP so the same row can be tapped again.
                if (sender is CollectionView cv)
                    cv.SelectedItem = null;

                // TAP = "Next Visit"
                // - If we preloaded a future visit id, open UpdateVisitPage
                // - Otherwise, schedule a new one for this student
                if (svm.NextFutureVisitId is int visitId)
                {
                    await GoToUpdateVisitAsync(visitId);
                }
                else
                {
                    await GoToAddVisitAsync(svm.Model.StudentId);
                }
            }
            catch
            {
                // Intentionally quiet for now.
                // Later: hook into your support logging pipeline.
            }
        }

        // ------------------------------------------------------------
        // SWIPE ACTIONS (Edit / Add Visit)
        // ------------------------------------------------------------

        private static Student? TryGetStudentFromSwipeSender(object sender)
        {
            if (sender is not SwipeItem swipeItem) return null;

            // Prefer CommandParameter if set
            if (swipeItem.CommandParameter is Student s1) return s1;

            // Otherwise BindingContext may be StudentViewModel
            if (swipeItem.BindingContext is StudentViewModel svm) return svm.Model;

            // Or BindingContext may be Student (depending on template)
            return swipeItem.BindingContext as Student;
        }

        private async void OnAddVisitSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                var student = TryGetStudentFromSwipeSender(sender);
                if (student is null) return;

                // Swipe "Add Visit" always schedules a new visit (intentional override).
                await GoToAddVisitAsync(student.StudentId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await DisplayAlert("Oops", "Something went wrong. Try again.", "OK");
            }
        }

        private async void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                var student = TryGetStudentFromSwipeSender(sender);
                if (student is null) return;

                await GoToEditStudentAsync(student.StudentId);
            }
            catch
            {
                await DisplayAlert("Something Broke", "Back up and try again", "OK");
            }
        }

        // ------------------------------------------------------------
        // PAGE LIFECYCLE
        // ------------------------------------------------------------
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Refresh list when returning from Add/Edit/Update visit pages.
            if (BindingContext is StudentsListViewModel vm)
                await vm.LoadAsync();
        }

        // ------------------------------------------------------------
        // ADD STUDENT BUTTON
        // ------------------------------------------------------------
        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            await GoToAddStudentAsync();
        }

        // ------------------------------------------------------------
        // LISTVIEW HANDLER (legacy / optional)
        // ------------------------------------------------------------
        private async void OnStudentSelected(object sender, SelectedItemChangedEventArgs e)
        {
            try
            {
                var student = e.SelectedItem as Student;
                if (student is null) return;

                // If you still have a ListView somewhere, we apply the same "Next Visit" rule.
                // But since ListView doesn't bind StudentViewModel here, we cannot know NextFutureVisitId
                // without querying. To avoid DB calls in the View, we keep legacy ListView as profile nav.
                await GoToStudentProfileAsync(student.StudentId);

                if (sender is ListView lv)
                    lv.SelectedItem = null;
            }
            catch
            {
                // optional log later
            }
        }
    }
}
