// StudentsMapPage.xaml.cs — displays a map of students with saved locations — 2026-01-15

using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
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

            // Best available "map is ready enough" signal in MAUI Maps
            StudentsMap.Loaded += OnMapLoaded;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Prevent duplicate handlers if the page is reused
            StudentsMap.Loaded -= OnMapLoaded;
        }

        private async void OnMapLoaded(object? sender, EventArgs e)
        {
            if (_loadedOnce) return;

            if (BindingContext is not StudentsMapViewModel vm)
                return;

            try
            {
                _loadedOnce = true;

                // 1) Load pins into VM
                await vm.RefreshPinsCommand.ExecuteAsync(null);

                // 2) Push pins into the actual Map control (Map.Pins is not bindable)
                SyncPinsToMap(vm);

                // 3) Center map (device location if possible, otherwise first pin)
                await CenterMapAsync(vm);
            }
            catch
            {
                // Swallow: VM already sets Status on failure
            }
        }

        private void SyncPinsToMap(StudentsMapViewModel vm)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StudentsMap.Pins.Clear();

                foreach (var pin in vm.Pins)
                {
                    if (pin?.Location is null)
                        continue;

                    // Defensive: skip junk (0,0) coords so we don't "successfully" pin the ocean
                    if (Math.Abs(pin.Location.Latitude) < 0.000001 &&
                        Math.Abs(pin.Location.Longitude) < 0.000001)
                        continue;

                    StudentsMap.Pins.Add(pin);
                }
            });
        }

        private async Task CenterMapAsync(StudentsMapViewModel vm)
        {
            // Try to center on current device location first
            try
            {
                var req = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
                var loc = await Geolocation.Default.GetLocationAsync(req);

                if (loc is not null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StudentsMap.MoveToRegion(
                            MapSpan.FromCenterAndRadius(
                                new Location(loc.Latitude, loc.Longitude),
                                Distance.FromMiles(2)));
                    });

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
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StudentsMap.MoveToRegion(
                        MapSpan.FromCenterAndRadius(
                            vm.Pins[0].Location,
                            Distance.FromMiles(5)));
                });
            }
        }
    }
}
