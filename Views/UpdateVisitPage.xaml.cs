// ---------------------------------------------------------------------------------------------------------------------
// UpdateVisitPage.xaml.cs
//
// PURPOSE
// - Shell host for editing/managing one existing visit.
// - Receives visitId from Shell query params.
// - Wires page lifecycle, ViewModel loading, and navigation responses.
//
// DESIGN RULES
// - This page is for an EXISTING visit.
// - ViewModel owns load/save/cancel/reschedule logic.
// - Page owns navigation and user-facing prompts.
// - Reschedule does not edit datetime in place.
//   It should go to Calendar in reschedule mode.
//
// ---------------------------------------------------------------------------------------------------------------------

using System;
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

    private async void OnSaveCompleted()
    {
        await DisplayAlert("Save Changes", "Changes saved.", "OK");
    }

    private async void OnCancelCompleted()
    {
        await DisplayAlert("Cancel Visit", "Visit canceled.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    private async void OnOutcomeCompleted(string message)
    {
        await DisplayAlert("Visit Outcome", message, "OK");
    }

    private async void OnRescheduleRequested(int visitId, int studentId)
    {
        try
        {
            // -----------------------------------------------------------------
            // LOCKED RULE:
            // Reschedule goes through Calendar.
            // The current visit is not edited in place.
            //
            // We pass both visitId and studentId so Calendar can support
            // a proper reschedule flow when that mode is wired.
            // -----------------------------------------------------------------
            await Shell.Current.GoToAsync(
                $"{nameof(MyCalendarPage)}?mode=reschedule&visitId={visitId}&studentId={studentId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlert("Reschedule", "Could not open Calendar.", "OK");
        }
    }

    private async void OnOperationFailed(string message)
    {
        await DisplayAlert("Visit", message, "OK");
    }
}
