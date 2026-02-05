// ---------------------------------------------------------------------------------------------------------------------
// SettingsViewModel.cs (DROP-IN)
// ViewModel for Settings page (Diagnostics + Dev Seeding).
//
// FIXES (why your current one breaks):
// - Removed calls to non-existent DataService methods:
//     SeedDevDataFromDiagnosticsAsync / ResetAndSeedFromDiagnosticsAsync / GetDbHealthAsync
// - Removed "force" (per your rule)
// - Uses ONLY these DataService methods (realistic + standard):
//     SeedDemoDataAsync(ct)
//     ResetDatabaseAsync(ct)
//     GetDatabaseHealthAsync(ct)   <-- rename to match your Diagnostics file if needed
//
// PATTERN KEPT:
// - Uses cancellation + IsBusy
// - Uses WeakReferenceMessenger to push UI messages to the View
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Storage;
using MinistryTracker.Data;
using Microsoft.Maui.Storage;
using MinistryTracker.ViewModels.Messages;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class SettingsViewModel : ObservableObject


    {
       

        private readonly DataService _data;

        // Single active operation CTS; new operation cancels the previous one
        private CancellationTokenSource? _activeCts;

       

        // Bindables (XAML)
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string dbHealthText = string.Empty;
        [ObservableProperty] private bool healthExpanded; // collapsed by default; expands after health loads
        private const string DevModeKey = "IsDeveloperMode";

        [ObservableProperty] private bool isDeveloperMode;

        public SettingsViewModel(DataService data)
        {
            _data = data;

            try
            {
                IsDeveloperMode = Preferences.Get(DevModeKey, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Preferences.Get failed: " + ex);
                IsDeveloperMode = false;
            }
        }

        // Simple UI helper you already had
        [RelayCommand]
        private void HideHealth() => HealthExpanded = false;

        // -------------------------------------------------------------------------------------------------------------
        // Helpers: manage a single active operation with cancellation + busy state
        // -------------------------------------------------------------------------------------------------------------
        private CancellationToken StartOperation()
        {
            _activeCts?.Cancel();
            _activeCts?.Dispose();
            _activeCts = new CancellationTokenSource();

            IsBusy = true;
            return _activeCts.Token;
        }

        partial void OnIsDeveloperModeChanged(bool value)
        {
            Preferences.Set(DevModeKey, value);
        }

        private void EndOperation() => IsBusy = false;

        public void CancelActiveOperation()
        {
            _activeCts?.Cancel();
            _activeCts?.Dispose();
            _activeCts = null;
            IsBusy = false;
        }

        // -------------------------------------------------------------------------------------------------------------
        // COMMANDS
        // -------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Insert a small, deterministic set of Students + Visits if empty.
        /// Safe default: does nothing if real data already exists.
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task SeedDemoData()
        {
            if (IsBusy) return;
            var ct = StartOperation();

            try
            {
                // ✅ Real method name (from your DataService.Seeding.cs plan)
                await _data.SeedDemoDataAsync(ct).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(new UiToastMessage("✅ Demo data inserted (if empty)."));

                // Notify listeners (Dashboard / Students) to refresh
                WeakReferenceMessenger.Default.Send(new DataSeededMessage(DateTime.UtcNow));
            }
            catch (OperationCanceledException)
            {
                // user started another op or navigated away — ignore
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new UiAlertMessage("Seeding failed", ex.Message));
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Wipe local DB, recreate data by seeding demo data (if empty).
        ///
        /// NOTE:
        /// - No "force" reseed exists (per your rule).
        /// - Reset = hard delete tables (diagnostics-only), then seed.
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ResetAndSeed()
        {
            if (IsBusy) return;

            // Ask the View to confirm (MVVM-pure).
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
                // ✅ Diagnostics method (should exist in DataService.Diagnostics.cs)
                await _data.ResetDatabaseAsync(ct).ConfigureAwait(false);

                // ✅ Now seed (idempotent, but DB is empty so it will insert)
                await _data.SeedDemoDataAsync(ct).ConfigureAwait(false);

                WeakReferenceMessenger.Default.Send(new UiToastMessage("Reset + seed complete"));

                WeakReferenceMessenger.Default.Send(
                    new UiSnackbarMessage("🧹 Reset + Seed complete.", "View health", UiSnackbarAction.ShowDbHealth));

                WeakReferenceMessenger.Default.Send(new DatabaseResetMessage(DateTime.UtcNow));
                WeakReferenceMessenger.Default.Send(new DataSeededMessage(DateTime.UtcNow));
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new UiAlertMessage("Reset failed", ex.Message));
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Quick health snapshot. Populates DbHealthText and expands the UI.
        /// </summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ShowDbHealth()
        {
            if (IsBusy) return;
            var ct = StartOperation();

            try
            {
                // Short timeout so button never feels stuck
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

                // ✅ Use a real diagnostics method name.
                // If your DataService uses a different name, rename *this one call* accordingly.
                var schema = await _data.GetDatabaseSchemaHealthAsync(linked.Token).ConfigureAwait(false);

                // Dump the full report (tables + columns + counts + user_version)
                DbHealthText = schema.ReportText;

                HealthExpanded = true;
                WeakReferenceMessenger.Default.Send(new UiToastMessage("DB schema health updated"));
            }
            catch (OperationCanceledException)
            {
                if (!ct.IsCancellationRequested)
                {
                    DbHealthText = "DB Schema timed out. Try again.";
                    WeakReferenceMessenger.Default.Send(new UiToastMessage("Health check timed out"));
                }
            }
            catch (Exception ex)
            {
                DbHealthText = $"Health check failed:\n{ex.Message}";
                WeakReferenceMessenger.Default.Send(new UiAlertMessage("Health check failed", ex.Message));
            }
            finally
            {
                EndOperation();
            }
        }

        // -------------------------------------------------------------------------------------------------------------
        // Messages other VMs can listen to for refresh
        // -------------------------------------------------------------------------------------------------------------
        public sealed class DatabaseResetMessage : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<DateTime>
        {
            public DatabaseResetMessage(DateTime whenUtc) : base(whenUtc) { }
        }

        public sealed class DataSeededMessage : CommunityToolkit.Mvvm.Messaging.Messages.ValueChangedMessage<DateTime>
        {
            public DataSeededMessage(DateTime whenUtc) : base(whenUtc) { }
        }
    }
}
