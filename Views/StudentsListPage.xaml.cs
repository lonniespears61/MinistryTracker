// ---------------------------------------------------------------------------------------------------------------------
// StudentsListPage.xaml.cs
// PURPOSE
// - Code-behind for the Students list (Shell tab).
// - Handles swipe actions, hint banner, and VM event subscriptions.
//
// WHAT CHANGED
// - Now subscribes to vm.ScheduleConflictDetected and calls DisplayActionSheet.
//   This is correct: DisplayActionSheet is a Page API — it belongs in the View.
// - Now subscribes to vm.RequestNavigate and calls Shell.Current.GoToAsync.
//   This is correct: Shell navigation is a View concern.
// - Subscribes in OnAppearing, unsubscribes in OnDisappearing (prevents memory leaks).
//
// WHAT DID NOT CHANGE
// - Swipe handlers and nav helpers for Edit/Schedule remain in code-behind.
//   These are View-driven interactions (swipe gestures), not VM-initiated ones.
//   The swipe handlers call GoToEditStudent directly — that is fine because the
//   swipe event already carries the student; no VM data lookup is needed first.
//   (Schedule swipe goes through the VM because the VM checks for conflicts.)
// - Swipe hint banner logic (Preferences read/write) is UI-only state.
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
    private const string PrefKey_SwipeHintDismissed = "StudentsList.InteractionHintDismissed.v2";

    private readonly StudentsListViewModel _vm;

    public StudentsListPage(StudentsListViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load data via the VM
        await _vm.LoadAsync();

        // Show swipe hint until dismissed
        UpdateSwipeHintBannerVisibility();

        // Subscribe to VM events while the page is visible
        _vm.ScheduleConflictDetected += OnScheduleConflictDetected;
        _vm.RequestNavigate += OnRequestNavigate;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Always unsubscribe — prevents memory leaks and duplicate handlers
        _vm.ScheduleConflictDetected -= OnScheduleConflictDetected;
        _vm.RequestNavigate -= OnRequestNavigate;
    }

    // =========================================================================
    // VM EVENT HANDLERS
    // =========================================================================

    /// <summary>
    /// VM detected a scheduling conflict. View shows the action sheet and
    /// calls back with the user's choice.
    /// VIOLATION FIXED: DisplayActionSheet is now correctly in the View.
    /// </summary>
    private async void OnScheduleConflictDetected(
        string conflictMessage, int studentId, int existingVisitId, Action<string?> callback)
    {
        var choice = await DisplayActionSheet(
            "Visit already scheduled",
            "Cancel",
            null,
            "Edit existing",
            "Replace it");

        callback(choice);
    }

    /// <summary>
    /// VM wants to navigate somewhere. View calls Shell.
    /// VIOLATION FIXED: Shell.GoToAsync is now correctly in the View.
    /// </summary>
    private async void OnRequestNavigate(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    // =========================================================================
    // SWIPE NAVIGATION HELPERS
    // These are View-level because swipe events carry the data directly (no
    // async data lookup needed for Edit). Schedule goes through the VM because
    // the VM must check for conflicts first.
    // =========================================================================

    private static Task GoToEditStudentAsync(int studentId) =>
        Shell.Current.GoToAsync($"{nameof(EditStudentPage)}?studentId={studentId}");

    private static Task GoToScheduleVisitCalendarAsync(int studentId) =>
        Shell.Current.GoToAsync($"{nameof(MyCalendarPage)}?mode=schedule&studentId={studentId}");

    private static Task GoToAddStudentAsync() =>
        Shell.Current.GoToAsync(nameof(AddStudentPage));

    private static Task GoToStudentProfileAsync(int studentId) =>
        Shell.Current.GoToAsync($"{nameof(StudentProfilePage)}?studentId={studentId}");

    // =========================================================================
    // SWIPE ITEM HELPERS
    // =========================================================================

    private static Student? TryGetStudentFromSwipeSender(object sender)
        => sender is SwipeItem swipeItem ? swipeItem.CommandParameter as Student : null;

    private static void CloseContainingSwipeView(Element element)
    {
        Element? current = element;
        while (current is not null && current is not SwipeView)
            current = current.Parent;
        (current as SwipeView)?.Close();
    }

    // =========================================================================
    // SWIPE HANDLERS
    // =========================================================================

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
            // Goes through the VM — conflict check requires a DataService call
            await GoToScheduleVisitCalendarAsync(student.StudentId);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await DisplayAlert("Something went wrong", "Please try again.", "OK");
        }
    }

    // =========================================================================
    // SWIPE HINT BANNER — UI-only state, correct in code-behind
    // =========================================================================

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

    private void OnShowListHelpClicked(object sender, EventArgs e)
        => SwipeHintBanner.IsVisible = true;

    // =========================================================================
    // ADD STUDENT BUTTON
    // =========================================================================

    private async void OnAddStudentClicked(object sender, EventArgs e)
        => await GoToAddStudentAsync();

    private async void OnStudentSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        var student = e.CurrentSelection
            .OfType<StudentViewModel>()
            .FirstOrDefault();

        if (student?.Model is null)
            return;

        await GoToStudentProfileAsync(student.Model.StudentId);
    }
}
