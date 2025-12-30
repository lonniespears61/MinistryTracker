using System;
using System.Linq;
using CommunityToolkit.Maui.Alerts;            // Toast, Snackbar
using CommunityToolkit.Mvvm.Messaging;         // WeakReferenceMessenger
using Microsoft.Maui.ApplicationModel;         // MainThread
using Microsoft.Maui.Controls;
using MinistryTracker.ViewModels;
using MinistryTracker.ViewModels.Messages;

namespace MinistryTracker.Views;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;
    private bool _messengerRegistered;

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;

        // You want Home, not the default back arrow
        NavigationPage.SetHasBackButton(this, false);
    }

    private static Page? GetCurrentPage()
        => Application.Current?.Windows.FirstOrDefault()?.Page;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_messengerRegistered) return;
        _messengerRegistered = true;

        // --- Toasts ---
        WeakReferenceMessenger.Default.Register<UiToastMessage>(this, async (_, msg) =>
        {
            await MainThread.InvokeOnMainThreadAsync(() => Toast.Make(msg.Value).Show());
        });

        // --- Alerts ---
        WeakReferenceMessenger.Default.Register<UiAlertMessage>(this, async (_, msg) =>
        {
            var (title, message, cancel) = msg.Value;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = GetCurrentPage();
                if (page is null) return;
                await page.DisplayAlert(title, message, cancel);
            });
        });

        // --- Snackbars ---
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

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var sb = Snackbar.Make(text, onClicked, actionText, TimeSpan.FromSeconds(4));
                _ = sb.Show();
            });
        });

        // --- Confirm (Yes/No) dialogs with callback to VM ---
        WeakReferenceMessenger.Default.Register<UiConfirmMessage>(this, async (_, msg) =>
        {
            var ok = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = GetCurrentPage();
                if (page is null) return false;

                return await page.DisplayAlert(msg.Title, msg.Message, msg.Accept, msg.Cancel);
            });

            msg.SetResult(ok);
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _vm.CancelActiveOperation();

        WeakReferenceMessenger.Default.Unregister<UiToastMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiAlertMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiSnackbarMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiConfirmMessage>(this);

        _messengerRegistered = false;
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync(animated: true);
    }
}
