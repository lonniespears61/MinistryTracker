using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    /// <summary>
    /// Dashboard page displaying summary information for the ministry.
    /// </summary>
    public partial class DashboardPage : ContentPage
    {
        private readonly DashboardViewModel _vm;

        public DashboardPage(DashboardViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _vm.LoadAsync();
        }

        // Toolbar "Settings" button (matches XAML: Clicked="OnSettingsClicked")
        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            var page = MauiProgram.Services.GetRequiredService<SettingsPage>();
            await Navigation.PushAsync(page);
        }

        // Tap on the "Active Students" card (matches XAML TapGestureRecognizer)
        private async void OnActiveStudentsTapped(object sender, TappedEventArgs e)
        {
            var page = MauiProgram.Services.GetRequiredService<StudentsListPage>();
            await Navigation.PushAsync(page);
        }
    }
}
