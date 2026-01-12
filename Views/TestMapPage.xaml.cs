using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace MinistryTracker.Views;

public partial class TestMapPage : ContentPage
{
    public TestMapPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
       
        // Center the map on current location (if available)
        try
        {
            var loc = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

            if (loc != null)
            {
                MapControl.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        new Location(loc.Latitude, loc.Longitude),
                        Distance.FromMiles(2)));

                MapControl.Pins.Clear();

                MapControl.Pins.Add(new Pin
                {
                    Label = "Test Student",
                    Address = "Pinned from code",
                    Location = new Location(loc.Latitude + 0.001, loc.Longitude + 0.001),
                    Type = PinType.Place
                });

            }
        }
        catch
        {
            // Keep it simple for now—if location fails, map still should render tiles.
        }
    }
}
