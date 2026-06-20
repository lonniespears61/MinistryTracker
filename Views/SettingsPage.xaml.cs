using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.ViewModels;
using MinistryTracker.ViewModels.Messages;

namespace MinistryTracker.Views;

public partial class SettingsPage : ContentPage
{
    private const int DeveloperUnlockTapCount = 7;

    private readonly SettingsViewModel _vm;
    private bool _messengerRegistered;
    private int _versionTapCount;

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    private static Page? GetCurrentPage()
        => Application.Current?.Windows.FirstOrDefault()?.Page;

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _ = _vm.InitializeSchemaInfoAsync();

        if (_messengerRegistered) return;
        _messengerRegistered = true;

        WeakReferenceMessenger.Default.Register<UiToastMessage>(this, async (_, msg) =>
        {
            await MainThread.InvokeOnMainThreadAsync(() => Toast.Make(msg.Value).Show());
        });

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

        WeakReferenceMessenger.Default.Register<UiSnackbarMessage>(this, async (_, msg) =>
        {
            var (text, actionText, action) = msg.Value;

            Action? onClicked = action switch
            {
                UiSnackbarAction.ShowDbHealth => () =>
                {
                    if (_vm.ShowDbHealthCommand.CanExecute(null))
                        _ = _vm.ShowDbHealthCommand.ExecuteAsync(null);
                },
                _ => null
            };

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var sb = Snackbar.Make(text, onClicked, actionText, TimeSpan.FromSeconds(4));
                _ = sb.Show();
            });
        });

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
        _vm.DisableDeveloperMode();

        WeakReferenceMessenger.Default.Unregister<UiToastMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiAlertMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiSnackbarMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiConfirmMessage>(this);

        _messengerRegistered = false;
        _versionTapCount = 0;
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnFeedbackClicked(object sender, EventArgs e)
    {
        var feedbackPage = MauiProgram.Services.GetRequiredService<FeedbackPage>();
        await Navigation.PushAsync(feedbackPage);
    }

    private async void OnWebsiteClicked(object sender, EventArgs e)
        => await Browser.Default.OpenAsync(
                new Uri("https://ministrytoolworks.com"),
                BrowserLaunchMode.SystemPreferred);

    private async void OnPrivacyPolicyClicked(object sender, EventArgs e)
        => await Browser.Default.OpenAsync(
                new Uri("https://ministrytoolworks.com/privacy.html"),
                BrowserLaunchMode.SystemPreferred);

    private void OnRemoveServiceDayClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: ServiceDaySettingRowViewModel row } &&
            _vm.RemoveServiceDayCommand.CanExecute(row))
        {
            _vm.RemoveServiceDayCommand.Execute(row);
        }
    }

    private async void OnVersionTapped(object sender, TappedEventArgs e)
    {
        if (_vm.IsDeveloperMode)
            return;

        _versionTapCount++;
        var remaining = DeveloperUnlockTapCount - _versionTapCount;

        if (remaining <= 0)
        {
            _vm.IsDeveloperMode = true;
            _versionTapCount = 0;
            await Toast.Make("Developer tools unlocked for this session.").Show();
            return;
        }

        if (remaining <= 3)
        {
            await Toast.Make($"{remaining} taps away from developer tools.").Show();
        }
    }
}
