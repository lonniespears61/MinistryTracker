using System;
using System.Diagnostics;
using System.Linq;                                // ✅ Needed for FirstOrDefault
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

// Note: Ensure you have the correct namespaces for your project.
namespace MinistryTracker.Views
{
    public partial class StudentsListPage : ContentPage
    {
        public StudentsListPage(StudentsListViewModel vm)

        {
            InitializeComponent();
            BindingContext = vm;
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

                var page = MauiProgram.Services.GetRequiredService<StudentProfilePage>();
                (page.BindingContext as StudentProfileViewModel)?.Load(selected);
                await Navigation.PushAsync(page);
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

        // ✅ Matches XAML: Invoked="OnAddVisitSwipeInvoked"
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

        // Optional: floating "+" button handler (if you have it wired in XAML)
        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            var page = MauiProgram.Services.GetRequiredService<AddStudentPage>();
            await Navigation.PushAsync(page);
        }
    }
}
