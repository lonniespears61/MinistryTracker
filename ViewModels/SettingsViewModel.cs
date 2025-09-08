// ---------------------------------------------------------------------------------------------------------------------
// SettingsViewModel.cs
// ViewModel for Settings page. Manual-only diagnostics + dev seeding.
// - Db health snapshot shown in-page (Editor bound to DbHealthText)
// - Reset + Seed with clear success/failure UX
// - Toast/Snackbar feedback; never leaves the UI "stuck"
// - Simple messages so other VMs (Dashboard, Students) can auto-refresh
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Alerts;                   // Toast / Snackbar
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;           // [ObservableProperty]
using CommunityToolkit.Mvvm.Input;                    // [RelayCommand]
using CommunityToolkit.Mvvm.Messaging;                // WeakReferenceMessenger
using MinistryTracker.Data;

namespace MinistryTracker.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly DataService _data;

        // ===== Bindables for XAML =====

        // Spinner + disabling buttons while work runs (see XAML: ActivityIndicator / IsEnabled bindings)
        [ObservableProperty] private bool isBusy;

        // What the "DB Health" Editor shows (SettingsPage.xaml binds Editor.Text to this)
        [ObservableProperty] private string dbHealthText = string.Empty;

        public SettingsViewModel(DataService data) => _data = data;

        // -------------------------------------------------------------------------------------------------------------
        // COMMANDS (bound in XAML as SeedDemoDataCommand, ResetAndSeedCommand, ShowDbHealthCommand)
        // -------------------------------------------------------------------------------------------------------------

        /// <summary>Insert a small, deterministic set of Students + Visits if empty. No overwrite.</summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task SeedDemoData()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Calls diagnostics façade → seeding (keeps UI dependency narrow)
                await _data.SeedDevDataFromDiagnosticsAsync(force: false);  // uses DataService.Seeding.cs under the hood
                await Toast.Make("✅ Demo data inserted (if empty).", ToastDuration.Short).Show();

                // Notify listeners (Dashboard / Students) to refresh
                WeakReferenceMessenger.Default.Send(new DataSeededMessage(DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Seeding failed", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Wipe local DB, recreate schema, then seed demo data. Handy for beta testers.</summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ResetAndSeed()
        {
            if (IsBusy) return;

            var ok = await App.Current.MainPage.DisplayAlert(
                "Reset Database",
                "This will wipe local data and recreate the DB, then seed demo data.\nContinue?",
                "Yes", "No");
            if (!ok) return;

            IsBusy = true;
            try
            {
                // Resets (closes DB, deletes/recreates file) → then seeds
                await _data.ResetAndSeedFromDiagnosticsAsync();

                await Toast.Make("Reset + seed complete").Show();

                // Optional UX: Snackbar with a quick action to show health immediately
                var snackbar = Snackbar.Make(
                    "🧹 Reset + Seed complete.",
                    async () =>
                    {
                        var h = await _data.GetDbHealthAsync();
                        DbHealthText = h.ToString();
                        await App.Current.MainPage.DisplayAlert("DB Health", DbHealthText, "OK");
                    },
                    "View health",
                    TimeSpan.FromSeconds(4));
                await snackbar.Show();

                // Broadcast to listeners so dashboards/lists repopulate
                WeakReferenceMessenger.Default.Send(new DatabaseResetMessage(DateTime.UtcNow));
                WeakReferenceMessenger.Default.Send(new DataSeededMessage(DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Reset failed", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>Quick health snapshot: exists/size, PRAGMA integrity, table list, and row counts.</summary>
        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task ShowDbHealth()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // Guard against slow I/O or a locked DB so the button never stays greyed out forever
                var healthTask = _data.GetDbHealthAsync();
                var winner = await Task.WhenAny(healthTask, Task.Delay(TimeSpan.FromSeconds(6)));
                if (winner != healthTask)
                {
                    DbHealthText = "DB health timed out. Try again.";
                    await Toast.Make("Health check timed out", ToastDuration.Short).Show();
                    return;
                }

                var health = await healthTask;               // finished
                DbHealthText = health.ToString();            // drives the Editor
                await Toast.Make("DB health updated", ToastDuration.Short).Show();
            }
            catch (Exception ex)
            {
                DbHealthText = $"Health check failed:\n{ex.Message}";
                await App.Current.MainPage.DisplayAlert("Health check failed", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // -------------------------------------------------------------------------------------------------------------
        // Lightweight messages any ViewModel can listen to for auto-refresh (Dashboard, Students, etc.)
        // Usage in other VMs:
        //     WeakReferenceMessenger.Default.Register<DatabaseResetMessage>(this, (_, __) => LoadAsync());
        //     WeakReferenceMessenger.Default.Register<DataSeededMessage>(this,  (_, __) => LoadAsync());
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
