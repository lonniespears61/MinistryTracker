// ---------------------------------------------------------------------------------------------------------------------
// StudentsListPage.xaml.cs
// PURPOSE
// - Code-behind for the Students list (Shell tab).
// - Handles swipe actions, hint banner, and VM event subscriptions.
//
// Scheduling always goes through VisitWorkflowCoordinator so eligibility,
// conflicts, replacement, and navigation behavior are identical everywhere.
// Swipe hint state remains UI-only.
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using MinistryTracker.Models;
using MinistryTracker.Services;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

public partial class StudentsListPage : ContentPage
{
    private const string PrefKey_SwipeHintDismissed = "StudentsList.InteractionHintDismissed.v2";

    private readonly StudentsListViewModel _vm;
    private readonly VisitWorkflowCoordinator _workflow;

    public StudentsListPage(
        StudentsListViewModel vm,
        VisitWorkflowCoordinator workflow)
    {
        InitializeComponent();
        _vm = vm;
        _workflow = workflow;
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

    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

    }

    // =========================================================================
    // SWIPE NAVIGATION HELPERS
    // These are View-level because swipe events carry the data directly (no
    // async data lookup needed for Edit). Schedule goes through the VM because
    // the VM must check for conflicts first.
    // =========================================================================

    private static Task GoToEditStudentAsync(int studentId) =>
        Shell.Current.GoToAsync($"{nameof(EditStudentPage)}?studentId={studentId}");

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

            await _workflow.BeginSchedulingAsync(this, student.StudentId);
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
