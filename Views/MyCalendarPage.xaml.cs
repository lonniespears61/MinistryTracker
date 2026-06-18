// ---------------------------------------------------------------------------------------------------------------------
// MyCalendarPage.xaml.cs — My calendar page shell host — 2026-02-01
// Purpose: Loads VM, receives calendar context from Shell query params,
//          and performs navigation when VM raises scheduling/edit intents.
//
// DESIGN RULES
// - Calendar has three modes:
//   1) normal mode: view/manage existing visits
//   2) scheduling mode: entered from a known student to choose a date
//   3) reschedule mode: entered from UpdateVisit to move an existing visit
//
// - Student scheduling context should remain sticky until the user intentionally
//   leaves it or completes a different action.
// - Day selection is transient.
// - Page owns prompts / navigation.
// - ViewModel owns state and intent events.
// ---------------------------------------------------------------------------------------------------------------------

using System;
using MinistryTracker.Models.DTOs;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(StudentId), "studentId")]
[QueryProperty(nameof(VisitId), "visitId")]
public partial class MyCalendarPage : ContentPage
{
    private readonly MyCalendarViewModel _vm;

    private bool _loading;
    private bool _subscribed;

    public string? Mode { get; set; }
    public string? StudentId { get; set; }
    public string? VisitId { get; set; }

    public MyCalendarPage(MyCalendarViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_subscribed)
        {
            _vm.ScheduleVisitRequested += OnScheduleVisitRequested;
            _vm.RescheduleVisitRequested += OnRescheduleVisitRequested;
            _vm.ExistingVisitTapped += OnExistingVisitTapped;
            _subscribed = true;
        }

        _ = TryLoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_subscribed)
        {
            _vm.ScheduleVisitRequested -= OnScheduleVisitRequested;
            _vm.RescheduleVisitRequested -= OnRescheduleVisitRequested;
            _vm.ExistingVisitTapped -= OnExistingVisitTapped;
            _subscribed = false;
        }
    }

    private async System.Threading.Tasks.Task TryLoadAsync()
    {
        if (_loading)
            return;

        try
        {
            _loading = true;

            await _vm.LoadAsync();

            if (string.Equals(Mode, "schedule", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(StudentId, out var sid) &&
                sid > 0)
            {
                await _vm.BeginSchedulingForStudentAsync(sid);

                // Keep the student scheduling context, but clear any previous day
                // selection so backing out of Add Visit does not leave phantom state.
                _vm.ClearSelectedDay();
            }
            else if (string.Equals(Mode, "reschedule", StringComparison.OrdinalIgnoreCase) &&
                     int.TryParse(StudentId, out var rsid) &&
                     int.TryParse(VisitId, out var vid) &&
                     rsid > 0 &&
                     vid > 0)
            {
                await _vm.BeginRescheduleAsync(vid, rsid);

                // Same rule as schedule mode: keep context, clear transient date choice.
                _vm.ClearSelectedDay();
            }
            else
            {
                _vm.ClearSchedulingContext();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            _loading = false;
        }
    }

    private async void OnScheduleVisitRequested(int? studentId, DateTime date)
    {
        try
        {
            var dateString = date.ToString("yyyy-MM-dd");

            if (studentId is int sid && sid > 0)
            {
                await Shell.Current.GoToAsync($"{nameof(AddVisitPage)}?studentId={sid}&date={dateString}&returnTo=calendar");
                return;
            }

            await Shell.Current.GoToAsync($"{nameof(SelectStudentForVisitPage)}?date={dateString}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlert("Something Broke", "Back up and try again", "OK");
        }
    }

    private async void OnRescheduleVisitRequested(int visitId, int studentId, DateTime newDate)
    {
        try
        {
            var confirm = await DisplayAlert(
                "Reschedule Visit",
                $"Move this visit to {newDate.Date.Add(_vm.RescheduleTime):dddd, MMM d 'at' h:mm tt}?",
                "Yes",
                "No");

            if (!confirm)
                return;

            await _vm.CompleteRescheduleAsync(newDate);
            await DisplayAlert("Reschedule Visit", "Visit rescheduled.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlert("Error", "Reschedule failed.", "OK");
        }
    }

    private async void OnExistingVisitTapped(VisitWithStudent visit)
    {
        try
        {
            // Normal mode:
            // tapping an existing visit means open it.
            if (!_vm.IsSchedulingMode)
            {
                await OpenExistingVisitAsync(visit);
                return;
            }

            // Scheduling/rescheduling mode + same student:
            // opening the tapped visit is the most natural interpretation.
            if (_vm.SchedulingStudentId == visit.StudentId)
            {
                await OpenExistingVisitAsync(visit);
                return;
            }

            // Scheduling/rescheduling mode + different student:
            // preserve current student context unless the user explicitly switches.
            var currentStudentName = string.IsNullOrWhiteSpace(_vm.SchedulingStudentName)
                ? "current student"
                : _vm.SchedulingStudentName;

            var tappedStudentName = string.IsNullOrWhiteSpace(visit.StudentName)
                ? "this student"
                : visit.StudentName;

            var choice = await DisplayActionSheet(
                $"You're scheduling for {currentStudentName}. What would you like to do?",
                "Cancel",
                null,
                $"Schedule {currentStudentName} on this date",
                $"Open {tappedStudentName}'s Visit");

            if (choice == $"Schedule {currentStudentName} on this date")
            {
                if (_vm.SchedulingStudentId is int schedulingStudentId)
                {
                    OnScheduleVisitRequested(schedulingStudentId, visit.ScheduledDateTime.Date);
                }

                return;
            }

            if (choice == $"Open {tappedStudentName}'s Visit")
            {
                _vm.ClearSchedulingContext();
                await OpenExistingVisitAsync(visit);
                return;
            }

            // Cancel = keep current context and do nothing else.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlert("Something Broke", "Back up and try again", "OK");
        }
    }

    private void OnVisitSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        if (e.CurrentSelection.Count > 0 &&
            e.CurrentSelection[0] is VisitWithStudent visit)
        {
            OnExistingVisitTapped(visit);
        }
    }

    private async System.Threading.Tasks.Task OpenExistingVisitAsync(VisitWithStudent visit)
    {
        await Shell.Current.GoToAsync($"{nameof(UpdateVisitPage)}?visitId={visit.VisitId}");
    }
}
