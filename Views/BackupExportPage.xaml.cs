using Microsoft.Maui.ApplicationModel.DataTransfer;
using MinistryTracker.Services.Backup;

namespace MinistryTracker.Views;

public partial class BackupExportPage : ContentPage
{
    private readonly BackupExportService _backupExport;

    public BackupExportPage(BackupExportService backupExport)
    {
        InitializeComponent();
        _backupExport = backupExport ?? throw new ArgumentNullException(nameof(backupExport));
    }

    private void OnShowPinChanged(object sender, CheckedChangedEventArgs e)
    {
        BackupPinEntry.IsPassword = !e.Value;
        ConfirmBackupPinEntry.IsPassword = !e.Value;
    }

    private async void OnCreateBackupClicked(object sender, EventArgs e)
    {
        if (BusyOverlay.IsVisible)
            return;

        var backupPin = GetBackupPin();
        if (backupPin is null)
            return;

        var understood = await DisplayAlert(
            "Keep your Backup PIN",
            "You will need this 6-digit PIN to restore the backup on another device.",
            "I understand",
            "Cancel");

        if (!understood)
            return;

        await DisplayAlert(
            "Save off-device",
            "Save this backup somewhere outside this device. A backup stored only on this phone may be lost if the phone is lost, damaged, reset, or the app is removed.",
            "Continue");

        try
        {
            BusyOverlay.IsVisible = true;
            var result = await _backupExport.ExportAsync(backupPin);

            ClearBackupPin();

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Save MinistryTracker backup",
                File = new ShareFile(result.FilePath, "application/octet-stream")
            });

            await DisplayAlert(
                "Backup created",
                $"Encrypted backup created with {result.StudentCount} students and {result.VisitCount} visits. If you canceled the save/share screen, create another backup and choose an off-device location.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Backup failed", ex.Message, "OK");
        }
        finally
        {
            BusyOverlay.IsVisible = false;
            ClearBackupPin();
        }
    }

    private string? GetBackupPin()
    {
        var backupPin = BackupPinEntry.Text ?? string.Empty;
        var confirmation = ConfirmBackupPinEntry.Text ?? string.Empty;

        if (!BackupPin.IsValid(backupPin))
        {
            _ = DisplayAlert("Backup PIN needed", "Enter exactly 6 digits.", "OK");
            return null;
        }

        if (!string.Equals(backupPin, confirmation, StringComparison.Ordinal))
        {
            _ = DisplayAlert("PINs do not match", "Enter the same 6-digit PIN in both fields.", "OK");
            return null;
        }

        return backupPin;
    }

    private void ClearBackupPin()
    {
        BackupPinEntry.Text = string.Empty;
        ConfirmBackupPinEntry.Text = string.Empty;
    }
}
