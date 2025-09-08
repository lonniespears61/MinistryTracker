using CommunityToolkit.Mvvm.Messaging;
using MinistryTracker.ViewModels;
using MinistryTracker.ViewModels.Messages;

namespace MinistryTracker.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;

    // Page is constructed via DI:
    // builder.Services.AddTransient<SettingsViewModel>();
    // builder.Services.AddTransient<SettingsPage>();
    public SettingsPage(SettingsViewModel vm)
    {
        try
        {
            InitializeComponent();     // XAML parsing happens here
            BindingContext = vm;
        }
        catch (Exception ex)
        {
            // Print to debug output
            System.Diagnostics.Debug.WriteLine("SettingsPage error: " + ex);

            // Show it in-app (so you don’t need to dig in Output)
            Application.Current?.MainPage?.DisplayAlert(
                "SettingsPage error",
                ex.ToString(),
                "OK");

            throw; // keep rethrowing so debugger breaks as usual
        }
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // other registrations …

        // Confirm dialog
        WeakReferenceMessenger.Default.Register<UiConfirmMessage>(this, async (_, msg) =>
        {
            var ok = await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current!.MainPage!.DisplayAlert(msg.Title, msg.Message, msg.Accept, msg.Cancel));

            // pass the result back to the ViewModel
            msg.SetResult(ok);
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _vm.CancelActiveOperation();

        // unregister to avoid leaks or double-handling
        WeakReferenceMessenger.Default.Unregister<UiToastMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiAlertMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiSnackbarMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiConfirmMessage>(this);
    }

   
}
