namespace MinistryTracker.Utilities;

using Microsoft.Maui.ApplicationModel;

public static class LocationPermissionHelper
{
    public static async Task<bool> EnsureLocationPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status == PermissionStatus.Granted)
            return true;

        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        return status == PermissionStatus.Granted;
    }
}
