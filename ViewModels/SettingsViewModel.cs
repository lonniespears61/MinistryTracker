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
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Services;
using MinistryTracker.ViewModels.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ObservableCollection<ServiceDaySettingRowViewModel> ActiveServiceDays { get; } = new();

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string dbHealthText = string.Empty;

        [ObservableProperty]
        private bool healthExpanded;

        [ObservableProperty]
        private bool isDeveloperMode;

        [ObservableProperty]
        private bool betaTesterAgreementAccepted;

        [ObservableProperty]
        private bool includeDiagnosticsInFeedback;

        [ObservableProperty]
        private string? feedbackText;

        [ObservableProperty]
        private bool dataSharingAgreementAccepted;

        // =====================================================================
        // SCHEMA VERSION DISPLAY
        // =====================================================================

        [ObservableProperty]
        private int currentSchemaVersion;

        [ObservableProperty]
        private int appSchemaVersion;

        [ObservableProperty]
        private bool isMigrationRequired;

        [ObservableProperty]
        private double deleteAndReseedSliderValue;

        [ObservableProperty]
        private double resetSchemaAndDataSliderValue;

        [ObservableProperty]
        private double resetSchemaOnlySliderValue;

        // =====================================================================
        // CONSTRUCTOR
        // =====================================================================

        public SettingsViewModel(DataService data, SettingsService settingsService)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            IsDeveloperMode = false;

            LoadServiceDaySettings();
            LoadBetaFeedbackSettings();
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

        public bool CanAddServiceDay => ActiveServiceDays.Count < 21;

        // =====================================================================
        // SETTINGS LOAD / SAVE
        // =====================================================================

        public void LoadServiceDaySettings()
        {
            ServiceDays = _settingsService.GetServiceDaySettings();

            ActiveServiceDays.Clear();

            var rules = ServiceDays.ActiveDays
                .Where(x => x.Period != ServicePeriod.None)
                .OrderBy(x => GetDaySortOrder(x.Day))
                .ThenBy(x => x.Period)
                .ToList();

            if (rules.Count > 0)
            {
                foreach (var rule in rules)
                    ActiveServiceDays.Add(new ServiceDaySettingRowViewModel(rule.Day, rule.Period));
            }
            else
            {
                AddServiceDayRowIfConfigured(DayOfWeek.Saturday, ServiceDays.Saturday);
                AddServiceDayRowIfConfigured(DayOfWeek.Sunday, ServiceDays.Sunday);
                AddServiceDayRowIfConfigured(DayOfWeek.Monday, ServiceDays.Monday);
                AddServiceDayRowIfConfigured(DayOfWeek.Tuesday, ServiceDays.Tuesday);
                AddServiceDayRowIfConfigured(DayOfWeek.Wednesday, ServiceDays.Wednesday);
                AddServiceDayRowIfConfigured(DayOfWeek.Thursday, ServiceDays.Thursday);
                AddServiceDayRowIfConfigured(DayOfWeek.Friday, ServiceDays.Friday);
            }

            OnPropertyChanged(nameof(CanAddServiceDay));
        }

        private void LoadBetaFeedbackSettings()
        {
            BetaTesterAgreementAccepted = _settingsService.GetBetaTesterAgreementAccepted();
            IncludeDiagnosticsInFeedback = _settingsService.GetIncludeDiagnosticsInFeedback();
            DataSharingAgreementAccepted = _settingsService.GetDataSharingAgreementAccepted();
        }

        [RelayCommand]
        private void SaveServiceDaySettings()
        {
            var duplicateRule = ActiveServiceDays
                .GroupBy(x => new { x.Day, x.Period })
                .FirstOrDefault(g => g.Count() > 1)
                ?.Key;

            if (duplicateRule is not null)
            {
                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Duplicate Service Time", $"{duplicateRule.Day} {duplicateRule.Period} is listed more than once. Remove or change the duplicate before saving."));
                return;
            }

            ServiceDays = BuildServiceDaySettingsFromRows();
            _settingsService.SaveServiceDaySettings(ServiceDays);
            WeakReferenceMessenger.Default.Send(new UiToastMessage("Service day settings saved."));
        }

        [RelayCommand]
        private void AddServiceDay()
        {
            var nextDay = GetNextUnusedServiceDay();

            if (nextDay is null)
            {
                WeakReferenceMessenger.Default.Send(new UiToastMessage("All days are already added."));
                return;
            }

            ActiveServiceDays.Add(new ServiceDaySettingRowViewModel(nextDay.Value.Day, nextDay.Value.Period));
            OnPropertyChanged(nameof(CanAddServiceDay));
        }

        [RelayCommand]
        private void RemoveServiceDay(ServiceDaySettingRowViewModel? row)
        {
            if (row is null)
                return;

            ActiveServiceDays.Remove(row);
            OnPropertyChanged(nameof(CanAddServiceDay));
        }

        private void AddServiceDayRowIfConfigured(DayOfWeek day, ServicePeriod period)
        {
            if (period == ServicePeriod.None)
                return;

            ActiveServiceDays.Add(new ServiceDaySettingRowViewModel(day, period));
        }

        private (DayOfWeek Day, ServicePeriod Period)? GetNextUnusedServiceDay()
        {
            var usedRules = ActiveServiceDays
                .Select(x => (x.Day, x.Period))
                .ToHashSet();

            var preferredOrder = new[]
            {
                DayOfWeek.Saturday,
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
            };

            foreach (var day in preferredOrder)
            {
                var periods = new[]
                {
                    ServicePeriod.Morning,
                    ServicePeriod.Afternoon,
                    ServicePeriod.Evening
                };

                foreach (var period in periods)
                {
                    if (!usedRules.Contains((day, period)))
                        return (day, period);
                }
            }

            return null;
        }

        private ServiceDaySettings BuildServiceDaySettingsFromRows()
        {
            var settings = new ServiceDaySettings();

            foreach (var row in ActiveServiceDays)
            {
                if (row.Period == ServicePeriod.None)
                    continue;

                settings.ActiveDays.Add(new ServiceDayRule
                {
                    Day = row.Day,
                    Period = row.Period
                });

                switch (row.Day)
                {
                    case DayOfWeek.Sunday:
                        settings.Sunday = row.Period;
                        break;
                    case DayOfWeek.Monday:
                        settings.Monday = row.Period;
                        break;
                    case DayOfWeek.Tuesday:
                        settings.Tuesday = row.Period;
                        break;
                    case DayOfWeek.Wednesday:
                        settings.Wednesday = row.Period;
                        break;
                    case DayOfWeek.Thursday:
                        settings.Thursday = row.Period;
                        break;
                    case DayOfWeek.Friday:
                        settings.Friday = row.Period;
                        break;
                    case DayOfWeek.Saturday:
                        settings.Saturday = row.Period;
                        break;
                }
            }

            return settings;
        }

        private static int GetDaySortOrder(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Saturday => 0,
                DayOfWeek.Sunday => 1,
                DayOfWeek.Monday => 2,
                DayOfWeek.Tuesday => 3,
                DayOfWeek.Wednesday => 4,
                DayOfWeek.Thursday => 5,
                DayOfWeek.Friday => 6,
                _ => 7
            };
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

        partial void OnBetaTesterAgreementAcceptedChanged(bool value)
        {
            _settingsService.SaveBetaTesterAgreementAccepted(value);

            if (!value)
                IncludeDiagnosticsInFeedback = false;
        }

        partial void OnIncludeDiagnosticsInFeedbackChanged(bool value)
        {
            if (value && !BetaTesterAgreementAccepted)
            {
                IncludeDiagnosticsInFeedback = false;
                WeakReferenceMessenger.Default.Send(
                    new UiToastMessage("Accept the beta agreement before including diagnostics."));
                return;
            }

            _settingsService.SaveIncludeDiagnosticsInFeedback(value);
        }

        partial void OnDataSharingAgreementAcceptedChanged(bool value)
        {
            _settingsService.SaveDataSharingAgreementAccepted(value);
        }

        public void DisableDeveloperMode()
        {
            IsDeveloperMode = false;
        }

        partial void OnDeleteAndReseedSliderValueChanged(double value)
        {
            if (value < 100 || IsBusy) return;

            DeleteAndReseedSliderValue = 0;
            _ = DeleteAndReseedTestData();
        }

        partial void OnResetSchemaAndDataSliderValueChanged(double value)
        {
            if (value < 100 || IsBusy) return;

            ResetSchemaAndDataSliderValue = 0;
            _ = ResetDbSchemaAndData();
        }

        partial void OnResetSchemaOnlySliderValueChanged(double value)
        {
            if (value < 100 || IsBusy) return;

            ResetSchemaOnlySliderValue = 0;
            _ = ResetDbSchemaOnly();
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

        [RelayCommand(AllowConcurrentExecutions = false)]
        private async Task SendFeedback()
        {
            if (!BetaTesterAgreementAccepted)
            {
                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Beta Agreement Required", "Please accept the beta tester agreement before sending feedback."));
                return;
            }

            if (string.IsNullOrWhiteSpace(FeedbackText))
            {
                WeakReferenceMessenger.Default.Send(
                    new UiToastMessage("Add a short note before sending feedback."));
                return;
            }

            try
            {
                var body = await BuildFeedbackBodyAsync().ConfigureAwait(false);

                var message = new EmailMessage
                {
                    Subject = $"Ministry Tracker Beta Feedback - {AppInfo.Current.VersionString}",
                    Body = body,
                    BodyFormat = EmailBodyFormat.PlainText
                };

                await Email.Default.ComposeAsync(message).ConfigureAwait(false);
            }
            catch (FeatureNotSupportedException)
            {
                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Email Not Available", "This device does not have an email app available."));
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(
                    new UiAlertMessage("Feedback Failed", ex.Message));
            }
        }

        private async Task<string> BuildFeedbackBodyAsync()
        {
            var acceptedOn = _settingsService.GetBetaTesterAgreementAcceptedOnUtc();
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("Ministry Tracker Beta Feedback");
            sb.AppendLine();
            sb.AppendLine("Feedback:");
            sb.AppendLine(FeedbackText?.Trim());
            sb.AppendLine();
            sb.AppendLine("Beta tester agreement accepted: Yes");

            if (acceptedOn is not null)
                sb.AppendLine($"Accepted UTC: {acceptedOn:yyyy-MM-dd HH:mm:ss}");

            sb.AppendLine($"App version: {AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})");
            sb.AppendLine($"Platform: {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}");
            sb.AppendLine($"Device: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}");

            if (IncludeDiagnosticsInFeedback)
            {
                sb.AppendLine();
                sb.AppendLine("Diagnostics included by tester opt-in:");
                sb.AppendLine("This diagnostic section contains schema/version/table counts, not student or visit row contents.");
                sb.AppendLine();

                var schema = await _data.GetDatabaseSchemaHealthAsync().ConfigureAwait(false);
                sb.AppendLine(schema.ReportText);
            }
            else
            {
                sb.AppendLine();
                sb.AppendLine("Diagnostics included: No");
            }

            return sb.ToString();
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
