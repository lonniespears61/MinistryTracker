using Microsoft.Maui.Controls;
using MinistryTracker.ViewModels;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // ✅ REQUIRED

namespace MinistryTracker.Views;

public partial class AddVisitPage : ContentPage
{
    private readonly AddVisitViewModel _vm;
    private bool _dateRequested;
    private bool _timeRequested;

    public AddVisitPage(AddVisitViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _vm.SaveCompleted -= OnSaveCompleted;
        _vm.SaveFailed -= OnSaveFailed; // ✅ added consistency
    }

    private async void OnSaveCompleted()
    {
        // ✅ Ensure UI thread
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Shell.Current.GoToAsync("..");
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
}