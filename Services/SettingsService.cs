using MinistryTracker.Models;
using System.Text.Json;
using Microsoft.Maui.Storage;


namespace MinistryTracker.Services
{
    public class SettingsService
    {
        private const string ServiceDaySettingsKey = "service_day_settings";
        private const string BetaTesterAgreementAcceptedKey = "beta_tester_agreement_accepted";
        private const string BetaTesterAgreementAcceptedOnKey = "beta_tester_agreement_accepted_on_utc";
        private const string IncludeDiagnosticsInFeedbackKey = "include_diagnostics_in_feedback";
        private const string DataSharingAgreementAcceptedKey = "data_sharing_agreement_accepted";
        private const string DataSharingAgreementAcceptedOnKey = "data_sharing_agreement_accepted_on_utc";

        public ServiceDaySettings GetServiceDaySettings()
        {
            var json = Preferences.Get(ServiceDaySettingsKey, null);

            if (string.IsNullOrWhiteSpace(json))
                return new ServiceDaySettings();

            return JsonSerializer.Deserialize<ServiceDaySettings>(json)
                   ?? new ServiceDaySettings();
        }

        public void SaveServiceDaySettings(ServiceDaySettings settings)
        {
            var json = JsonSerializer.Serialize(settings);
            Preferences.Set(ServiceDaySettingsKey, json);
        }

        public bool GetBetaTesterAgreementAccepted()
        {
            return Preferences.Get(BetaTesterAgreementAcceptedKey, false);
        }

        public void SaveBetaTesterAgreementAccepted(bool accepted)
        {
            Preferences.Set(BetaTesterAgreementAcceptedKey, accepted);

            if (accepted)
                Preferences.Set(BetaTesterAgreementAcceptedOnKey, DateTime.UtcNow.ToString("O"));
            else
                Preferences.Remove(BetaTesterAgreementAcceptedOnKey);
        }

        public DateTime? GetBetaTesterAgreementAcceptedOnUtc()
        {
            var raw = Preferences.Get(BetaTesterAgreementAcceptedOnKey, null);

            return DateTime.TryParse(raw, out var parsed)
                ? parsed.ToUniversalTime()
                : null;
        }

        public bool GetIncludeDiagnosticsInFeedback()
        {
            return Preferences.Get(IncludeDiagnosticsInFeedbackKey, false);
        }

        public void SaveIncludeDiagnosticsInFeedback(bool includeDiagnostics)
        {
            Preferences.Set(IncludeDiagnosticsInFeedbackKey, includeDiagnostics);
        }

        public bool GetDataSharingAgreementAccepted()
        {
            return Preferences.Get(DataSharingAgreementAcceptedKey, false);
        }

        public void SaveDataSharingAgreementAccepted(bool accepted)
        {
            Preferences.Set(DataSharingAgreementAcceptedKey, accepted);

            if (accepted)
                Preferences.Set(DataSharingAgreementAcceptedOnKey, DateTime.UtcNow.ToString("O"));
            else
                Preferences.Remove(DataSharingAgreementAcceptedOnKey);
        }
    }
}
