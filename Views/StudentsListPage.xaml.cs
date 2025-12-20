using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;
using MinistryTracker.Views;


namespace MinistryTracker.Views
{
    public partial class StudentsListPage : ContentPage
    {
        public StudentsListPage(StudentsListViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        // -------- Common navigator ----------
        private async System.Threading.Tasks.Task NavigateToProfileAsync(Student student)
        {
            var sp = Application.Current?.Handler?.MauiContext?.Services;
            var profile = sp?.GetRequiredService<StudentProfilePage>();
            if (profile is null) return;

            profile.Init(student);                 // hydrate VM before navigation
            await Navigation.PushAsync(profile);   // uses ContentPage.Navigation
        }

        // -------- CollectionView handler ----------
        // XAML: SelectionChanged="OnStudentSelectionChanged"
        private async void OnStudentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var svm = e.CurrentSelection?.FirstOrDefault() as StudentViewModel;
                if (svm?.Model is null) return;

                await NavigateToProfileAsync(svm.Model);

                if (sender is CollectionView cv) cv.SelectedItem = null;
            }
            catch
            {
                // optional log
            }
        }

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

                await DisplayAlert("Add Visit", $"Add a visit for {student.Name}.", "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await DisplayAlert("Oops",
                    "Something went wrong. Try again.",
                    "OK");
            }
        }

        private async void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                var student = TryGetStudentFromSwipeSender(sender);
                if (student is null) return;

                var sp = Application.Current?.Handler?.MauiContext?.Services;
                var editPage = sp?.GetRequiredService<EditStudentPage>();
                if (editPage is null) return;

                editPage.Init(student);
                await Navigation.PushAsync(editPage);
            }
            catch
            {
                await DisplayAlert("Something Broke", "Back up and try again", "OK");
            }
        }


        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is StudentsListViewModel vm)
                await vm.LoadAsync();
        }
        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Add Student", "Add Student feature coming soon.", "OK");
        }


        // -------- ListView handler (if you use ListView) ----------
        // XAML: ItemSelected="OnStudentSelected"
        private async void OnStudentSelected(object sender, SelectedItemChangedEventArgs e)
        {
            try
            {
                var student = e.SelectedItem as Student;
                if (student is null) return;

                await NavigateToProfileAsync(student);

                if (sender is ListView lv) lv.SelectedItem = null;
            }
            catch { /* optionally log */ }
        }
    }
}
