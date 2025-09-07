using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly DataService _data;

        [ObservableProperty]
        private string _dbHealthText = "Tap 'Show DB Health' to fetch status.";

        public SettingsViewModel(DataService data)
        {
            _data = data;
        }

        [RelayCommand]
        private async Task ShowHealthAsync()
        {
            var h = await _data.GetDbHealthAsync();
            DbHealthText = h.ToString();
        }

        [RelayCommand]
        private async Task ReseedAsync()
        {
            await _data.ReseedAsync();
            await ShowHealthAsync();
        }

        [RelayCommand]
        private async Task ResetDbAsync()
        {
            // Optional: add a UI confirm in the page; keeping VM clean here.
            await _data.ResetDatabaseAsync();
            await ShowHealthAsync();
        }
    }
}
