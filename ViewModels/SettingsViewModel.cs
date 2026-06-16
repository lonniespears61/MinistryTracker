// ---------------------------------------------------------------------------------------------------------------------
// SettingsViewModel.cs
//
// PURPOSE
// - ViewModel for Settings page
// - Handles:
//   1. Developer / diagnostics actions
//   2. Database health display
//   3. Normal Service Days settings
//
// CHANGE NOTES (04/12/2026)
// - Added schema version visibility (DB vs App)
// - Added migration detection flag
// - Added ApplyMigrationCommand (stub for future use)
// - Split dev reset actions into:
//   * Delete & Reseed Test Data
//   * Reset DB (Schema + Data)
//   * Reset DB (Schema Only)
//
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Services;
using MinistryTracker.ViewModels.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly DataService _data;
        private readonly SettingsService _settingsService;

        private CancellationTokenSource? _activeCts;

        // =====================================================================
        // SETTINGS BINDABLES
        // =====================================================================

        [ObservableProperty]
        private ServiceDaySettings serviceDays = new();

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string dbHealthText = string.Empty;

        [ObservableProperty]
        private bool healthExpanded;

        [ObservableProperty]
        private bool isDeveloperMode;

        // =====================================================================
        // SCHEMA VERSION DISPLAY
        // =====================================================================

        [ObservableProperty]
        private int currentSchemaVersion;

        [ObservableProperty]
        private int appSchemaVersion;

        [ObservableProperty]
        private bool isMigrationRequired;

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public SettingsViewModel(DataService data, SettingsService settingsService)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            IsDeveloperMode = false;

            LoadServiceDaySettings();
        }

        // =====================================================================
        // INITIALIZATION
        // =====================================================================

        public async Task InitializeSchemaInfoAsync()
        {
            try
            {
                CurrentSchemaVersion = await _data.GetDatabaseSchemaVersionAsync().ConfigureAwait(false);
                AppSchemaVersion = _data.GetAppSchemaVersion();
                IsMigrationRequired = await _data.IsMigrationRequiredAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Schema info load failed: " + ex);
            }
        }

        // =====================================================================
        // PICKER SOURCES
        // =====================================================================

        public List<ServicePeriod> ServicePeriodValues =>
            Enum.GetValues<ServicePeriod>().ToList();

        // =====================================================================
        // SETTINGS LOAD / SAVE
        // =====================================================================

        public void LoadServiceDaySettings()
        {
            ServiceDays = _settingsService.GetServiceDaySettings();
        }

        [RelayCommand]
        private void SaveServiceDaySettings()
        {
            _settingsService.SaveServiceDaySettings(ServiceDays);
            WeakReferenceMessenger.Default.Send(new UiToastMessage("Service day settings saved."));
        }

        // =====================================================================
        // SIMPLE UI HELPERS
        // =====================================================================

        [RelayCommand]
        private void HideHealth() => HealthExpanded = false;

        partial void OnIsDeveloperModeChanged(bool value)
        {
            if (!value)
                HealthExpanded = false;
        }

        public void DisableDeveloperMode()
        {
            IsDeveloperMode = false;
        }

        // =====================================================================
        // OPERATION HELPERS
        // =====================================================================

        private CancellationToken StartOperation()
        {
            _activeCts?.Cancel();
            _activeCts?.Dispose();

            _activeCts = new CancellationTokenSource();
            IsBusy = true;

            return _activeCts.Token;
        }

        private void EndOperation()
        {
            IsBusy = false;
        }

        public void CancelActiveOperation()
        {
            _activeCts?.Cancel();
            _activeCts?.Dispose();
            _activeCts = null;
            IsBusy = false;
        }

        // =====================================================================
        // DEV / DIAGNOSTICS COMMANDS
        // =====================================================================

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task SeedDemoData()
        {
            if (IsBusy) return;

            var ct = StartOperation();

            try
            {
                await _data.SeedDemoDataAsync(ct).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(
                    new UiToastMessage("✅ Demo data inserted (if empty)."));

                WeakReferenceMessenger.Default.Send(
                    new DataSeededMessage(DateTime.UtcNow));

                await InitializeSchemaInfoAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore user cancellation / replaced operation
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Seeding failed", ex.Message));
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Delete rows and reseed test data.
        /// Keeps current schema intact.
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task DeleteAndReseedTestData()
        {
            if (IsBusy) return;

            var proceed = await ConfirmTypedAsync(
                "Delete & Reseed Test Data",
                "This will delete all students and visits, then reseed test data.\n\nType DELETE to continue.",
                "DELETE").ConfigureAwait(false);

            if (!proceed) return;

            var ct = StartOperation();

            try
            {
                await _data.ResetDatabaseAsync(ct).ConfigureAwait(false);
                await _data.SeedDemoDataAsync(ct).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(
                    new UiToastMessage("Delete + reseed complete"));

                WeakReferenceMessenger.Default.Send(
                    new UiSnackbarMessage("Delete + Reseed complete.", "View health", UiSnackbarAction.ShowDbHealth));

                WeakReferenceMessenger.Default.Send(
                    new DatabaseResetMessage(DateTime.UtcNow));

                WeakReferenceMessenger.Default.Send(
                    new DataSeededMessage(DateTime.UtcNow));

                await InitializeSchemaInfoAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Delete + reseed failed", ex.Message));
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Delete DB file, rebuild schema, then reseed.
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ResetDbSchemaAndData()
        {
            if (IsBusy) return;

            var proceed = await ConfirmTypedAsync(
                "Reset DB (Schema + Data)",
                "This will delete the local database, rebuild the schema, and reseed test data.\n\nType RESET to continue.",
                "RESET").ConfigureAwait(false);

            if (!proceed) return;

            var ct = StartOperation();

            try
            {
                await _data.FullResetDatabaseAndSeedAsync(ct).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(
                    new UiToastMessage("Schema reset + seed complete"));

                WeakReferenceMessenger.Default.Send(
                    new UiSnackbarMessage("Reset DB (Schema + Data) complete.", "View health", UiSnackbarAction.ShowDbHealth));

                WeakReferenceMessenger.Default.Send(
                    new DatabaseResetMessage(DateTime.UtcNow));

                WeakReferenceMessenger.Default.Send(
                    new DataSeededMessage(DateTime.UtcNow));

                await InitializeSchemaInfoAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Schema reset failed", ex.Message));
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Delete DB file and rebuild schema only.
        /// No seed data is inserted.
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ResetDbSchemaOnly()
        {
            if (IsBusy) return;

            var proceed = await ConfirmTypedAsync(
                "Reset DB (Schema Only)",
                "This will delete the local database and rebuild the schema without inserting test data.\n\nType RESET to continue.",
                "RESET").ConfigureAwait(false);

            if (!proceed) return;

            var ct = StartOperation();

            try
            {
                await _data.FullResetDatabaseAsync(ct).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(
                    new UiToastMessage("Schema-only reset complete"));

                WeakReferenceMessenger.Default.Send(
                    new UiSnackbarMessage("Reset DB (Schema Only) complete.", "View health", UiSnackbarAction.ShowDbHealth));

                WeakReferenceMessenger.Default.Send(
                    new DatabaseResetMessage(DateTime.UtcNow));

                await InitializeSchemaInfoAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Schema-only reset failed", ex.Message));
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Kept for compatibility while old button bindings still exist.
        /// Current behavior matches the old intent: clear rows, then seed.
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ResetAndSeed()
        {
            await DeleteAndReseedTestData().ConfigureAwait(false);
        }

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ShowDbHealth()
        {
            if (IsBusy) return;

            var ct = StartOperation();

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

                var schema = await _data.GetDatabaseSchemaHealthAsync(linked.Token).ConfigureAwait(false);

                DbHealthText = schema.ReportText;
                HealthExpanded = true;

                WeakReferenceMessenger.Default.Send(
                    new UiToastMessage("DB schema health updated"));

                await InitializeSchemaInfoAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (!ct.IsCancellationRequested)
                {
                    DbHealthText = "DB Schema timed out. Try again.";
                    WeakReferenceMessenger.Default.Send(
                        new UiToastMessage("Health check timed out"));
                }
            }
            catch (Exception ex)
            {
                DbHealthText = $"Health check failed:\n{ex.Message}";

                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Health check failed", ex.Message));
            }
            finally
            {
                EndOperation();
            }
        }

        // =====================================================================
        // MIGRATION (FUTURE)
        // =====================================================================

        [RelayCommand]
        private async Task ApplyMigration()
        {
            // Placeholder only.
            // Real migration flow stays in DataService.cs, not in diagnostics helpers.

            await Task.CompletedTask;
        }

        private async Task<bool> ConfirmTypedAsync(string title, string message, string requiredText)
        {
            var tcs = new TaskCompletionSource<string?>();

            WeakReferenceMessenger.Default.Send(new UiPromptMessage(
                title,
                message,
                response => tcs.TrySetResult(response),
                accept: "Continue",
                cancel: "Cancel",
                placeholder: requiredText,
                maxLength: requiredText.Length));

            var response = await tcs.Task.ConfigureAwait(false);

            return string.Equals(response?.Trim(), requiredText, StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================================
        // MESSAGES
        // =====================================================================

        public sealed class DatabaseResetMessage
            : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<DateTime>
        {
            public DatabaseResetMessage(DateTime whenUtc) : base(whenUtc) { }
        }

        public sealed class DataSeededMessage
            : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<DateTime>
        {
            public DataSeededMessage(DateTime whenUtc) : base(whenUtc) { }
        }
    }
}
