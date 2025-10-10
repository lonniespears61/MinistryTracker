// Views/StudentsListPage.xaml.cs
using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class StudentsListPage : ContentPage
    {
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
                await _vm.LoadAsync();
                StudentsCollection.SelectedItem = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StudentsListPage OnAppearing failed: {ex}");
            }
        }

        // Kept for completeness (tap opens profile if/when you re-enable tap UX)
        private async void OnStudentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedVm = e.CurrentSelection?.FirstOrDefault() as StudentViewModel;
                var selected = selectedVm?.Model;
                if (selected is null) return;

                if (sender is CollectionView cv) cv.SelectedItem = null;

                var page = MauiProgram.Services.GetRequiredService<StudentProfilePage>();
                if (page.BindingContext is StudentProfileViewModel profileVm)
                    profileVm.Load(selected);

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
                if (sender is SwipeItem swipe && swipe.CommandParameter is Student student)
                {
                    var page = MauiProgram.Services.GetRequiredService<EditStudentPage>();
                    if (page.BindingContext is EditStudentViewModel vm)
                        vm.Load(student);

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
                // 1) Try to get the Student from the SwipeItem.CommandParameter
                Student? student = null;
                if (sender is SwipeItem si && si.CommandParameter is Student sFromParam)
                {
                    student = sFromParam;
                }
                else if (sender is Element el && el.BindingContext is StudentViewModel svm && svm.Model is Student sFromVm)
                {
                    // 2) Fallback: get it from the row's BindingContext if CommandParameter wasn't set
                    student = sFromVm;
                }

                if (student is null)
                    throw new InvalidOperationException("No student found for Add Visit (CommandParameter not bound).");

                // 3) Resolve page from DI
                var page = MauiProgram.Services.GetRequiredService<AddVisitPage>();
                if (page is null) throw new InvalidOperationException("AddVisitPage is not registered in DI.");

                // 4) Prefill the VM via the page’s API (do NOT call a nonexistent PreselectStudent)
                page.Load(student);  // forwards to AddVisitViewModel.Load(student)  

                // 5) Navigate
                await Navigation.PushAsync(page);
            }
            catch (Exception ex)
            {
                // Visible error so we see precisely what failed on-device/emulator
                await DisplayAlert("Add Visit failed", ex.Message, "OK");
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }


        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            var page = MauiProgram.Services.GetRequiredService<AddStudentPage>();
            await Navigation.PushAsync(page);
        }
    }
}
