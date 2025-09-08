using CommunityToolkit.Mvvm.Messaging.Messages;

namespace MinistryTracker.ViewModels.Messages;

// Simple toast: just a string
public sealed class UiToastMessage : ValueChangedMessage<string>
{
    public UiToastMessage(string text) : base(text) { }
}

// Alert: title + message (+ optional cancel text)
public sealed class UiAlertMessage : ValueChangedMessage<(string Title, string Message, string Cancel)>
{
    public UiAlertMessage(string title, string message, string cancel = "OK")
        : base((title, message, cancel)) { }
}

// Snackbar with optional action that the VIEW can handle (no VM refs!)
public enum UiSnackbarAction
{
    None,
    ShowDbHealth // page will invoke ShowDbHealthCommand when tapped
}

public sealed class UiSnackbarMessage : ValueChangedMessage<(string Text, string ActionText, UiSnackbarAction Action)>
{
    public UiSnackbarMessage(string text, string actionText = "OK", UiSnackbarAction action = UiSnackbarAction.None)
        : base((text, actionText, action)) { }
}
