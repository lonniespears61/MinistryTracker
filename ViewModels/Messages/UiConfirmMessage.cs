using System;

namespace MinistryTracker.ViewModels.Messages;

/// <summary>
/// Message to request a Yes/No style confirmation dialog from the View.
/// The View calls SetResult(true/false) to return the user's choice.
/// </summary>
public sealed class UiConfirmMessage
{
    public string Title { get; }
    public string Message { get; }
    public string Accept { get; }
    public string Cancel { get; }

    // Callback the View must call to supply the result
    public Action<bool> SetResult { get; }

    public UiConfirmMessage(string title, string message, Action<bool> setResult, string accept = "Yes", string cancel = "No")
    {
        Title = title;
        Message = message;
        Accept = accept;
        Cancel = cancel;
        SetResult = setResult;
    }
}
