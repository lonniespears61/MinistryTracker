using System;
using System.Diagnostics;
using System.Linq; // FirstOrDefault
using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class StudentsListPage : ContentPage
    {
        // ✅ Keep a handle to the VM you bound
        private readonly StudentsListViewModel _vm;

        public StudentsListPage(StudentsListViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // ✅ Load the list data when the page shows
                await _vm.LoadAsync();

                // Let user tap the same row again
                StudentsCollection.SelectedItem = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StudentsListPage OnAppearing failed: {ex}");
            }
        }

        private async void OnStudentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selected = e.CurrentSelection?.FirstOrDefault() as Student;
                if (selected is null)
                    return;

                if (sender is CollectionView cv)
                    cv.SelectedItem = null;

                var page = MauiProgram.Services.GetRequiredService<StudentProfilePage>();
                (page.BindingContext as StudentProfileViewModel)?.Load(selected);
                await Navigation.PushAsync(page);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Selection navigation failed: {ex}");
            }
        }

        private async void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItem swipe &&
                    swipe.CommandParameter is Student student)
                {
                    var page = MauiProgram.Services.GetRequiredService<EditStudentPage>();
                    (page.BindingContext as EditStudentViewModel)?.Load(student);
                    await Navigation.PushAsync(page);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Edit swipe failed: {ex}");
            }
        }

        private async void OnAddVisitSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItem swipe &&
                    swipe.CommandParameter is Student student)
                {
                    var page = MauiProgram.Services.GetRequiredService<AddVisitPage>();
                    (page.BindingContext as AddVisitViewModel)?.Load(student);
                    await Navigation.PushAsync(page);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Add Visit swipe failed: {ex}");
            }
        }

        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            var page = MauiProgram.Services.GetRequiredService<AddStudentPage>();
            await Navigation.PushAsync(page);
        }
    }
}
