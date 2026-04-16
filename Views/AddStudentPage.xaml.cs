using MinistryTracker.ViewModels;
using System;
using Microsoft.Maui.ApplicationModel;

namespace MinistryTracker.Views;

public partial class AddStudentPage : ContentPage
{
    private readonly AddStudentViewModel _vm;

    public AddStudentPage(AddStudentViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Reset keeps Add Student as a fresh-entry page each time it opens.
        _vm.Reset();

        // ViewModel owns save/location logic.
        // The page owns prompts, alerts, and navigation.
        _vm.SaveCompleted += OnSaveCompleted;
        _vm.SaveFailed += OnSaveFailed;
        _vm.AddressReplacementSuggested += OnAddressReplacementSuggested;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _vm.SaveCompleted -= OnSaveCompleted;
        _vm.SaveFailed -= OnSaveFailed;
        _vm.AddressReplacementSuggested -= OnAddressReplacementSuggested;
    }

    private async void OnSaveCompleted()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // Saving succeeds before any next-step prompt.
            // This keeps the record committed even if the user chooses not to schedule.
            var scheduleVisit = await DisplayAlert(
                "Student Saved",
                "Would you like to schedule a visit now?",
                "Yes",
                "No");

            if (scheduleVisit)
            {
                // Scheduling flow is the next wiring step.
                // Keep this branch explicit so the later route hookup is isolated here.
                await DisplayAlert(
                    "Next Step",
                    "Visit scheduling will be wired next.",
                    "OK");
            }

            await Shell.Current.GoToAsync("..");
        });
    }

    private async void OnSaveFailed(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Error", message, "OK");
        });
    }

    private async void OnAddressReplacementSuggested(string? currentAddress, string suggestedAddress)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // GPS is stored as the return location.
            // Address replacement stays user-confirmed because reverse geocoding can be wrong.
            var message =
                $"Current: {currentAddress ?? "(blank)"}{Environment.NewLine}{Environment.NewLine}" +
                $"GPS suggests: {suggestedAddress}{Environment.NewLine}{Environment.NewLine}" +
                "Use the GPS-derived address instead?";

            var useSuggested = await DisplayAlert(
                "Confirm Address",
                message,
                "Yes",
                "No");

            if (useSuggested)
            {
                _vm.ApplySuggestedGpsAddress();
            }
            else
            {
                _vm.KeepTypedAddress();
            }
        });
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}