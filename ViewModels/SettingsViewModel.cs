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
// DESIGN RULES
// - Keep diagnostics/dev tools working
// - Keep settings persistence simple
// - Service-day preferences are user settings, not database entities
//
// NOTES
// - Uses WeakReferenceMessenger for UI messages already established in this page flow
// - Uses SettingsService for normal service day preferences
//
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Storage;
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

        // Single active operation CTS; new operation cancels the previous one
        private CancellationTokenSource? _activeCts;

        private const string DevModeKey = "IsDeveloperMode";

        // =====================================================================
        // SETTINGS BINDABLES
        // =====================================================================

        /// <summary>
        /// Per-day normal service preferences.
        /// Each day can be None / Morning / Afternoon / Evening.
        /// </summary>
        [ObservableProperty]
        private ServiceDaySettings serviceDays = new();

        /// <summary>
        /// Busy flag for diagnostics/dev operations.
        /// </summary>
        [ObservableProperty]
        private bool isBusy;

        /// <summary>
        /// Database health report text shown in Settings UI.
        /// </summary>
        [ObservableProperty]
        private string dbHealthText = string.Empty;

        /// <summary>
        /// Controls whether the DB health section is expanded.
        /// </summary>
        [ObservableProperty]
        private bool healthExpanded;

        /// <summary>
        /// Toggles developer-mode UI visibility.
        /// </summary>
        [ObservableProperty]
        private bool isDeveloperMode;

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public SettingsViewModel(DataService data, SettingsService settingsService)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            try
            {
                IsDeveloperMode = Preferences.Get(DevModeKey, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Preferences.Get failed: " + ex);
                IsDeveloperMode = false;
            }

            LoadServiceDaySettings();
        }

        // =====================================================================
        // PICKER SOURCES
        // =====================================================================

        /// <summary>
        /// Picker source for normal service day period selection.
        /// </summary>
        public List<ServicePeriod> ServicePeriodValues =>
            Enum.GetValues<ServicePeriod>().ToList();

        // =====================================================================
        // SETTINGS LOAD / SAVE
        // =====================================================================

        /// <summary>
        /// Load persisted service-day settings.
        /// </summary>
        public void LoadServiceDaySettings()
        {
            ServiceDays = _settingsService.GetServiceDaySettings();
        }

        /// <summary>
        /// Save persisted service-day settings.
        /// </summary>
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
            Preferences.Set(DevModeKey, value);
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

        /// <summary>
        /// Insert demo data if DB is effectively empty.
        /// </summary>
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
        /// Reset local DB, then seed demo data again.
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ResetAndSeed()
        {
            if (IsBusy) return;

            var tcs = new TaskCompletionSource<bool>();

            WeakReferenceMessenger.Default.Send(new UiConfirmMessage(
                title: "Reset Database",
                message: "This will wipe local data and recreate the DB, then seed demo data.\nContinue?",
                setResult: confirmed => tcs.TrySetResult(confirmed),
                accept: "Yes",
                cancel: "No"));

            var proceed = await tcs.Task.ConfigureAwait(false);
            if (!proceed) return;

            var ct = StartOperation();

            try
            {
                await _data.ResetDatabaseAsync(ct).ConfigureAwait(false);
                await _data.SeedDemoDataAsync(ct).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(
                    new UiToastMessage("Reset + seed complete"));

                WeakReferenceMessenger.Default.Send(
                    new UiSnackbarMessage("🧹 Reset + Seed complete.", "View health", UiSnackbarAction.ShowDbHealth));

                WeakReferenceMessenger.Default.Send(
                    new DatabaseResetMessage(DateTime.UtcNow));

                WeakReferenceMessenger.Default.Send(
                    new DataSeededMessage(DateTime.UtcNow));
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Reset failed", ex.Message));
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Load authoritative DB schema/health report.
        /// </summary>
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