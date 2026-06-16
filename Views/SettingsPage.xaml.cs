using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
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

        WeakReferenceMessenger.Default.Register<UiPromptMessage>(this, async (_, msg) =>
        {
            var result = await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = GetCurrentPage();
                if (page is null) return null;

                return await page.DisplayPromptAsync(
                    msg.Title,
                    msg.Message,
                    msg.Accept,
                    msg.Cancel,
                    msg.Placeholder,
                    msg.MaxLength,
                    null,
                    msg.InitialValue);
            });

            msg.SetResult(result);
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
        WeakReferenceMessenger.Default.Unregister<UiPromptMessage>(this);

        _messengerRegistered = false;
    }

    private async void OnDoneClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
