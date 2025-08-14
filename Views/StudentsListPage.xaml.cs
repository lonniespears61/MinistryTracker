// StudentsListPage.xaml.cs
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using MinistryTracker.Models;

namespace MinistryTracker.Views
{
    public partial class StudentsListPage : ContentPage
    {
        // ... your existing ctor & search/selection handlers ...

        private async void OnEditSwipeInvoked(object sender, EventArgs e)
        {
            try
            {
                if (sender is SwipeItem swipe &&
                    swipe.CommandParameter is Student student)
                {
                    await Navigation.PushAsync(new EditStudentPage(student));
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
                    await Navigation.PushAsync(new AddVisitPage(student));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Add Visit swipe failed: {ex}");
            }
        }
    }
}
