// ---------------------------------------------------------------------------------------------------------------------
// StudentsListPage.xaml.cs — Students list interactions (Shell tab) — 2026-02-03
// Purpose:
//   • Handle swipe-only actions for student rows (Edit / Schedule)
//   • Show an in-page swipe hint banner on every visit until user dismisses it
// Notes:
//   • No database calls in the View
//   • Dismissal state stored via Preferences (local key/value)
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

public partial class StudentsListPage : ContentPage
{
    private const string PrefKey_SwipeHintDismissed = "StudentsList.SwipeHintDismissed";

    public StudentsListPage(StudentsListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    // ------------------------------------------------------------
    // Navigation helpers
    // ------------------------------------------------------------

    private static Task GoToEditStudentAsync(int studentId) =>
        Shell.Current.GoToAsync($"{nameof(EditStudentPage)}?studentId={studentId}");

    private static Task GoToScheduleVisitCalendarAsync(int studentId) =>
        Shell.Current.GoToAsync($"{nameof(MyCalendarPage)}?mode=schedule&studentId={studentId}");

    private static Task GoToAddStudentAsync() =>
        Shell.Current.GoToAsync(nameof(AddStudentPage));

    // ------------------------------------------------------------
    // Swipe helpers
    // ------------------------------------------------------------

    private static Student? TryGetStudentFromSwipeSender(object sender)
        => sender is SwipeItem swipeItem ? swipeItem.CommandParameter as Student : null;

    private static void CloseContainingSwipeView(Element element)
    {
        Element? current = element;

        while (current is not null && current is not SwipeView)
            current = current.Parent;

        (current as SwipeView)?.Close();
    }

    // ------------------------------------------------------------
    // Swipe handlers
    // ------------------------------------------------------------

    private async void OnEditSwipeInvoked(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("EDIT SWIPE FIRED");

            var student = TryGetStudentFromSwipeSender(sender);
            if (student is null) return;

            CloseContainingSwipeView((Element)sender);

            await GoToEditStudentAsync(student.StudentId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await DisplayAlert("Something went wrong", "Please try again.", "OK");
        }
    }

    private async void OnScheduleVisitSwipeInvoked(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("SCHEDULE SWIPE FIRED");

            var student = TryGetStudentFromSwipeSender(sender);
            if (student is null) return;

            CloseContainingSwipeView((Element)sender);

            await GoToScheduleVisitCalendarAsync(student.StudentId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await DisplayAlert("Something went wrong", "Please try again.", "OK");
        }
    }

    // ------------------------------------------------------------
    // Page lifecycle
    // ------------------------------------------------------------

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is StudentsListViewModel vm)
            await vm.LoadAsync();

        // Show tip banner on every visit until dismissed.
        UpdateSwipeHintBannerVisibility();
    }

    // ------------------------------------------------------------
    // Swipe hint banner
    // ------------------------------------------------------------

    private void UpdateSwipeHintBannerVisibility()
    {
        var dismissed = Preferences.Default.Get(PrefKey_SwipeHintDismissed, false);
        SwipeHintBanner.IsVisible = !dismissed;
    }

    private void OnDismissSwipeHintClicked(object sender, EventArgs e)
    {
        Preferences.Default.Set(PrefKey_SwipeHintDismissed, true);
        SwipeHintBanner.IsVisible = false;
    }

    // ------------------------------------------------------------
    // UI actions
    // ------------------------------------------------------------

    private async void OnAddStudentClicked(object sender, EventArgs e)
        => await GoToAddStudentAsync();
}
