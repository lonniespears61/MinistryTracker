// ---------------------------------------------------------------------------------------------------------------------
// StudentsListPage.xaml.cs — Students list interactions (Shell tab) — 2026-02-01
// Purpose:
//   • Handle swipe-only actions for student rows
//   • Swipe right  → Edit Student
//   • Swipe left   → Schedule Visit
//   • Tap does NOT navigate (visual focus only)
// Notes:
//   • Navigation is handled via Shell routes
//   • No database calls are made from this view
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

public partial class StudentsListPage : ContentPage
{
    // ------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------

    public StudentsListPage(StudentsListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    // ------------------------------------------------------------
    // Navigation helpers (centralized for clarity)
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

    /// <summary>
    /// Extracts the Student model from a SwipeItem sender.
    /// MAUI uses SwipeItem (not SwipeItemView).
    /// </summary>
    private static Student? TryGetStudentFromSwipeSender(object sender)
    {
        return sender is SwipeItem swipeItem
            ? swipeItem.CommandParameter as Student
            : null;
    }

    /// <summary>
    /// Walks up the visual tree and closes the containing SwipeView.
    /// MAUI does not auto-close on execute.
    /// </summary>
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
            if (student is null)
                return;

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
            if (student is null)
                return;

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
    }

    // ------------------------------------------------------------
    // UI actions
    // ------------------------------------------------------------

    private async void OnAddStudentClicked(object sender, EventArgs e)
        => await GoToAddStudentAsync();
}
