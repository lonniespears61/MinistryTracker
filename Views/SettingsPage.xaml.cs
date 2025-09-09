using CommunityToolkit.Maui.Alerts;            // Toast, Snackbar
using CommunityToolkit.Mvvm.Messaging;         // WeakReferenceMessenger
using Microsoft.Maui.ApplicationModel;          // MainThread
using MinistryTracker.ViewModels;
using MinistryTracker.ViewModels.Messages;

namespace MinistryTracker.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;

    // Construct via DI:
    // builder.Services.AddTransient<SettingsViewModel>();
    // builder.Services.AddTransient<SettingsPage>();
    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // --- Toasts ---
        WeakReferenceMessenger.Default.Register<UiToastMessage>(this, async (_, msg) =>
        {
            await MainThread.InvokeOnMainThreadAsync(() => Toast.Make(msg.Value).Show());
        });

        // --- Alerts ---
        WeakReferenceMessenger.Default.Register<UiAlertMessage>(this, async (_, msg) =>
        {
            var (title, message, cancel) = msg.Value;
            await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current!.MainPage!.DisplayAlert(title, message, cancel));
        });

        // --- Snackbars (sync Action wrapper around async work) ---
        WeakReferenceMessenger.Default.Register<UiSnackbarMessage>(this, async (_, msg) =>
        {
            var (text, actionText, action) = msg.Value;

            Action? onClicked = action switch
            {
                UiSnackbarAction.ShowDbHealth => () =>
                {
                    if (_vm.ShowDbHealthCommand.CanExecute(null))
                        _ = _vm.ShowDbHealthCommand.ExecuteAsync(null); // fire-and-forget
                }
                ,
                _ => null
            };

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var sb = Snackbar.Make(text, onClicked, actionText, TimeSpan.FromSeconds(4));
                await sb.Show();
            });
        });

        // --- Confirm (Yes/No) dialogs with callback to VM ---
        WeakReferenceMessenger.Default.Register<UiConfirmMessage>(this, async (_, msg) =>
        {
            var ok = await MainThread.InvokeOnMainThreadAsync(() =>
                Application.Current!.MainPage!.DisplayAlert(msg.Title, msg.Message, msg.Accept, msg.Cancel));

            msg.SetResult(ok); // return result to VM
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Cancel any in-flight VM operation so background DB work doesn't linger
        _vm.CancelActiveOperation();

        // Unregister to avoid leaks / duplicate handlers on re-navigation
        WeakReferenceMessenger.Default.Unregister<UiToastMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiAlertMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiSnackbarMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiConfirmMessage>(this);
    }
}
