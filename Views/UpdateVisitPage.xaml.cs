// ---------------------------------------------------------------------------------------------------------------------
// UpdateVisitPage.xaml.cs
//
// PURPOSE
// - Shell host for editing/managing one existing visit.
// - Receives visitId from Shell query params.
// - Wires page lifecycle, ViewModel loading, and navigation responses.
//
// FIX
// - All page-owned UI work is now forced onto the main thread.
// - This prevents Android crash:
//   "Can't create handler inside thread ... that has not called Looper.prepare()"
//
// DESIGN RULES
// - This page is for an EXISTING visit.
// - ViewModel owns load/save/cancel/reschedule logic.
// - Page owns navigation and user-facing prompts.
// - Reschedule does not edit datetime in place.
//   It goes to Calendar in reschedule mode.
//
// ---------------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.Maui.ApplicationModel;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

[QueryProperty(nameof(VisitId), "visitId")]
public partial class UpdateVisitPage : ContentPage
{
    private readonly UpdateVisitViewModel _vm;
    private bool _subscribed;

    public string? VisitId { get; set; }

    public UpdateVisitPage(UpdateVisitViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_subscribed)
        {
            _vm.SaveCompleted += OnSaveCompleted;
            _vm.CancelCompleted += OnCancelCompleted;
            _vm.OutcomeCompleted += OnOutcomeCompleted;
            _vm.RescheduleRequested += OnRescheduleRequested;
            _vm.OperationFailed += OnOperationFailed;
            _subscribed = true;
        }

        if (!int.TryParse(VisitId, out var parsedVisitId) || parsedVisitId <= 0)
        {
            _vm.StatusMessage = "Visit id is missing or invalid.";
            return;
        }

        await _vm.LoadAsync(parsedVisitId);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_subscribed)
        {
            _vm.SaveCompleted -= OnSaveCompleted;
            _vm.CancelCompleted -= OnCancelCompleted;
            _vm.OutcomeCompleted -= OnOutcomeCompleted;
            _vm.RescheduleRequested -= OnRescheduleRequested;
            _vm.OperationFailed -= OnOperationFailed;
            _subscribed = false;
        }
    }

    private void OnSaveCompleted()
    {
        MainThread.BeginInvokeOnMainThread(
            async () => await DisplayAlert("Save Changes", "Changes saved.", "OK"));
    }

    private void OnCancelCompleted()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert("Cancel Visit", "Visit canceled.", "OK");
            await Shell.Current.GoToAsync("..");
        });
    }

    private void OnOutcomeCompleted(string message)
    {
        MainThread.BeginInvokeOnMainThread(
            async () => await DisplayAlert("Visit Outcome", message, "OK"));
    }

    private void OnRescheduleRequested(int visitId, int studentId)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(MyCalendarPage)}?mode=reschedule&visitId={visitId}&studentId={studentId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                await DisplayAlert("Reschedule", "Could not open Calendar.", "OK");
            }
        });
    }

    private void OnOperationFailed(string message)
    {
        MainThread.BeginInvokeOnMainThread(
            async () => await DisplayAlert("Visit", message, "OK"));
    }
}
