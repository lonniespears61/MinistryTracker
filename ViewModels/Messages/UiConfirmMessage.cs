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

public sealed class UiPromptMessage
{
    public string Title { get; }
    public string Message { get; }
    public string Accept { get; }
    public string Cancel { get; }
    public string Placeholder { get; }
    public string? InitialValue { get; }
    public int MaxLength { get; }
    public Action<string?> SetResult { get; }

    public UiPromptMessage(
        string title,
        string message,
        Action<string?> setResult,
        string accept = "OK",
        string cancel = "Cancel",
        string placeholder = "",
        string? initialValue = null,
        int maxLength = -1)
    {
        Title = title;
        Message = message;
        Accept = accept;
        Cancel = cancel;
        Placeholder = placeholder;
        InitialValue = initialValue;
        MaxLength = maxLength;
        SetResult = setResult;
    }
}
