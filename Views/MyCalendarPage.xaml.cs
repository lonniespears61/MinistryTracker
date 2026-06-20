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
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Services;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(StudentId), "studentId")]
[QueryProperty(nameof(VisitId), "visitId")]
[QueryProperty(nameof(ReplaceVisitId), "replaceVisitId")]
public partial class MyCalendarPage : ContentPage
{
    private readonly MyCalendarViewModel _vm;
    private readonly VisitWorkflowCoordinator _workflow;

    private bool _loading;
    private bool _subscribed;
    private int? _pendingReplaceVisitId;

    public string? Mode { get; set; }
    public string? StudentId { get; set; }
    public string? VisitId { get; set; }
    public string? ReplaceVisitId { get; set; }

    public MyCalendarPage(
        MyCalendarViewModel vm,
        VisitWorkflowCoordinator workflow)
    {
        InitializeComponent();
        _vm = vm;
        _workflow = workflow;
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
                _pendingReplaceVisitId =
                    int.TryParse(ReplaceVisitId, out var replaceVisitId) &&
                    replaceVisitId > 0
                        ? replaceVisitId
                        : null;

                await _vm.BeginSchedulingForStudentAsync(sid);
            }
            else if (string.Equals(Mode, "reschedule", StringComparison.OrdinalIgnoreCase) &&
                     int.TryParse(StudentId, out var rsid) &&
                     int.TryParse(VisitId, out var vid) &&
                     rsid > 0 &&
                     vid > 0)
            {
                _pendingReplaceVisitId = null;
                await _vm.BeginRescheduleAsync(vid, rsid);
            }
            else
            {
                _pendingReplaceVisitId = null;
                _vm.ClearSchedulingContext();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            Mode = null;
            StudentId = null;
            VisitId = null;
            ReplaceVisitId = null;
            _loading = false;
        }
    }

    private async void OnScheduleVisitRequested(int? studentId, DateTime date)
    {
        try
        {
            if (studentId is int sid && sid > 0)
            {
                await _workflow.ContinueSchedulingAsync(
                    sid,
                    date,
                    _pendingReplaceVisitId);
                return;
            }

            await _workflow.SelectStudentForDateAsync(date);
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
            await _workflow.CompleteExistingVisitAsync(studentId);
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
            // While Calendar owns a pending student/visit scheduling context,
            // agenda entries are display-only and must not replace that flow.
            if (_vm.IsSchedulingMode && _vm.SchedulingStudentId is not null)
                return;

            await OpenExistingVisitAsync(visit);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlert("Something Broke", "Back up and try again", "OK");
        }
    }

    private async void OnVisitSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        if (e.CurrentSelection.Count == 0)
            return;

        if (_vm.IsSchedulingMode && _vm.SchedulingStudentId is not null)
        {
            var action = _vm.IsRescheduleMode ? "rescheduling" : "scheduling";
            var studentName = string.IsNullOrWhiteSpace(_vm.SchedulingStudentName)
                ? "the selected student"
                : _vm.SchedulingStudentName;

            await Toast.Make(
                    $"You are {action} a visit for {studentName}. Finish or go back before opening another visit.",
                    ToastDuration.Long)
                .Show();
            return;
        }

        if (e.CurrentSelection[0] is VisitWithStudent visit)
        {
            OnExistingVisitTapped(visit);
        }
    }

    private async System.Threading.Tasks.Task OpenExistingVisitAsync(VisitWithStudent visit)
    {
        await Shell.Current.GoToAsync($"{nameof(UpdateVisitPage)}?visitId={visit.VisitId}");
    }
}
