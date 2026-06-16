using MinistryTracker.Models;
using System.Text.Json;
using Microsoft.Maui.Storage;


namespace MinistryTracker.Services
{
    public class SettingsService
    {
        private const string Key = "service_day_settings";

        public ServiceDaySettings GetServiceDaySettings()
        {
            var json = Preferences.Get(Key, null);

            if (string.IsNullOrWhiteSpace(json))
                return new ServiceDaySettings();

            return JsonSerializer.Deserialize<ServiceDaySettings>(json)
                   ?? new ServiceDaySettings();
        }

        public void SaveServiceDaySettings(ServiceDaySettings settings)
        {
            var json = JsonSerializer.Serialize(settings);
            Preferences.Set(Key, json);
        }
    }
}