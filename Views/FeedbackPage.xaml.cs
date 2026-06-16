using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using MinistryTracker.ViewModels;
using MinistryTracker.ViewModels.Messages;

namespace MinistryTracker.Views;

public partial class FeedbackPage : ContentPage
{
    private bool _messengerRegistered;

    public FeedbackPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

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
                await DisplayAlert(title, message, cancel);
            });
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        WeakReferenceMessenger.Default.Unregister<UiToastMessage>(this);
        WeakReferenceMessenger.Default.Unregister<UiAlertMessage>(this);

        _messengerRegistered = false;
    }
}
