using MinistryTracker.ViewModels;
using System;
using Microsoft.Maui.ApplicationModel; // ✅ REQUIRED

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

        _vm.Reset();

        _vm.SaveCompleted += OnSaveCompleted;
        _vm.SaveFailed += OnSaveFailed;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _vm.SaveCompleted -= OnSaveCompleted;
        _vm.SaveFailed -= OnSaveFailed;
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

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}