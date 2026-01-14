// File: StudentsMapPage.xaml.cs
// Purpose: Displays a map of students with saved locations
// Created: 2026-01-14

using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class StudentsMapPage : ContentPage
    {
        private bool _loadedOnce;

        public StudentsMapPage(StudentsMapViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_loadedOnce) return;

            if (BindingContext is not StudentsMapViewModel vm)
                return;

            try
            {
                // 1) Load pins
                await vm.RefreshPinsCommand.ExecuteAsync(null);

                // 2) Center map (device location if possible, otherwise first pin)
                await CenterMapAsync(vm);

                _loadedOnce = true;
            }
            catch
            {
                // swallow: VM already sets Status on failure
            }
        }

        private async Task CenterMapAsync(StudentsMapViewModel vm)
        {
            // This assumes your XAML has: <maps:Map x:Name="StudentsMap" ... />

            // Try to center on current device location first
            try
            {
                var req = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
                var loc = await Geolocation.Default.GetLocationAsync(req);

                if (loc is not null)
                {
                    StudentsMap.MoveToRegion(
                        MapSpan.FromCenterAndRadius(
                            new Location(loc.Latitude, loc.Longitude),
                            Distance.FromMiles(2)));

                    return;
                }
            }
            catch
            {
                // permissions off / location unavailable -> fall back to pins
            }

            // Fallback: first pin
            if (vm.Pins.Count > 0 && vm.Pins[0].Location is not null)
            {
                StudentsMap.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        vm.Pins[0].Location,
                        Distance.FromMiles(5)));
            }
        }
    }
}
