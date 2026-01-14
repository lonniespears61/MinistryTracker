using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
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

            try
            {
                await _vm.LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                // Optional: await DisplayAlert("Oops", "Dashboard failed to load.", "OK");
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement ve)
                await TapAnimateAsync(ve);

            var page = MauiProgram.Services.GetRequiredService<SettingsPage>();
            await Navigation.PushAsync(page);
        }
        private async void OnTestMapClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(StudentsMapPage));
        }

        private async void OnAboutClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement ve)
                await TapAnimateAsync(ve);

            var page = MauiProgram.Services.GetRequiredService<AboutPage>();
            await Navigation.PushAsync(page);
        }
        private static async Task TapAnimateAsync(VisualElement view)
        {
            // Quick “press” feel: shrink a bit, then pop back.
            try
            {
                await view.ScaleTo(0.90, 70, Easing.CubicOut);
                await view.ScaleTo(1.00, 120, Easing.SpringOut);
            }
            catch
            {
                // If the page is disappearing during navigation, ignore animation failures.
            }
        }

       
    }
}
