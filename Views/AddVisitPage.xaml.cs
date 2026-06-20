using Microsoft.Maui.Controls;
using MinistryTracker.ViewModels;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // ✅ REQUIRED
using MinistryTracker.Services;

namespace MinistryTracker.Views;

public partial class AddVisitPage : ContentPage
{
    private readonly AddVisitViewModel _vm;
    private readonly VisitWorkflowCoordinator _workflow;
    private bool _dateRequested;
    private bool _timeRequested;
    private bool _canceling;

    public AddVisitPage(
        AddVisitViewModel vm,
        VisitWorkflowCoordinator workflow)
    {
        InitializeComponent();
        _vm = vm;
        _workflow = workflow;
        BindingContext = _vm;

        Loaded += (_, __) => TryOpenDateAsync();
        VisitDatePicker.HandlerChanged += (_, __) => TryOpenDateAsync();
        VisitTimePicker.HandlerChanged += (_, __) =>
        {
            if (_timeRequested) TryOpenTimeAsync();
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        TryOpenDateAsync();

        _vm.SaveCompleted += OnSaveCompleted;
        _vm.SaveFailed += OnSaveFailed; // ✅ added consistency
        _vm.CancelRequested += OnCancelRequested;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _vm.SaveCompleted -= OnSaveCompleted;
        _vm.SaveFailed -= OnSaveFailed; // ✅ added consistency
        _vm.CancelRequested -= OnCancelRequested;
    }

    private async void OnSaveCompleted()
    {
        // Saving a new visit completes the scheduling workflow, regardless of
        // where it started. Use an absolute tab route so Calendar, student
        // selection, profile, and other intermediate pages are removed.
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await _workflow.CompleteNewVisitAsync();
        });
    }

    private async void OnSaveFailed(string message)
    {
        // ✅ Ensure UI thread
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Error", message, "OK");
        });
    }

    private async void TryOpenDateAsync()
    {
        if (_dateRequested) return;

        _dateRequested = true;
        await Task.Delay(200);

        if (VisitDatePicker?.Handler is null)
        {
            _dateRequested = false;
            return;
        }

        VisitDatePicker.Focus();
    }

    private async void TryOpenTimeAsync()
    {
        await Task.Delay(150);

        if (VisitTimePicker?.Handler is null) return;

        VisitTimePicker.Focus();
    }

    private void OnVisitDateSelected(object sender, DateChangedEventArgs e)
    {
        if (BindingContext is not AddVisitViewModel vm) return;

        vm.VisitDate = e.NewDate.Date;
        VisitTimePicker.IsEnabled = true;

        _timeRequested = true;
        TryOpenTimeAsync();
    }
    private async void OnCancelRequested() => await CancelAsync();

    protected override bool OnBackButtonPressed()
    {
        if (_vm.CancelCommand.CanExecute(null))
            _vm.CancelCommand.Execute(null);
        return true;
    }

    private Task CancelAsync() =>
        CancelOnceAsync();

    private async Task CancelOnceAsync()
    {
        if (_canceling)
            return;

        _canceling = true;
        await _workflow.CancelNewVisitAsync(_vm.CancelTo);
    }
}
