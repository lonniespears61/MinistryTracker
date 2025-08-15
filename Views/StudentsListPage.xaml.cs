using System;
using System.Diagnostics;
using System.Linq;                                // ✅ Needed for FirstOrDefault
using Microsoft.Maui.Controls;
using MinistryTracker.Models;

namespace MinistryTracker.Views
{
    public partial class StudentsListPage : ContentPage
    {
        // If you already have a DI-injected VM ctor, keep it.
        // Otherwise you can also have a parameterless ctor that resolves the VM.
        public StudentsListPage()
        {
            InitializeComponent();
            // If you’re not using DI yet, set the BindingContext in XAML or here.
            // BindingContext = new StudentsListViewModel(App.Database); // pre-DI style
        }

        // ✅ Matches XAML: SelectionChanged="OnStudentSelectionChanged"
        private async void OnStudentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selected = e.CurrentSelection?.FirstOrDefault() as Student;
                if (selected is null)
                    return;

                // Clear selection so the same item can be tapped again
                if (sender is CollectionView cv)
                    cv.SelectedItem = null;

                // 👉 Navigate to the profile page by passing the Student.
                //    The page will create its own ViewModel using this Student.
                await Navigation.PushAsync(new StudentProfilePage(selected));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Selection navigation failed: {ex}");
            }
        }

        // ✅ Matches XAML: Invoked="OnEditSwipeInvoked"
        private async void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItem swipe &&
                    swipe.CommandParameter is Student student)
                {
                    // 👉 Keep it simple: pass the Student to the page.
                    await Navigation.PushAsync(new EditStudentPage(student));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Edit swipe failed: {ex}");
            }
        }

        // ✅ Matches XAML: Invoked="OnAddVisitSwipeInvoked"
        private async void OnAddVisitSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItem swipe &&
                    swipe.CommandParameter is Student student)
                {
                    await Navigation.PushAsync(new AddVisitPage(student));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Add Visit swipe failed: {ex}");
            }
        }

        // Optional: floating "+" button handler (if you have it wired in XAML)
        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddStudentPage());
        }
    }
}
