using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MinistryTracker.Platforms.Android;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                           ConfigChanges.Orientation |
                           ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Window is null)
            return;

        // Edge-to-edge is supported on Android 11 / API 30+.
        // Using OperatingSystem guards helps the analyzer understand
        // the platform/version check more reliably than raw Build.VERSION checks.
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Window.SetDecorFitsSystemWindows(false);
        }

        // setStatusBarColor is deprecated and has no effect on Android 15 / API 35+.
        // Keep it only for older supported Android versions.
        if (OperatingSystem.IsAndroidVersionAtLeast(21) &&
            !OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            Window.SetStatusBarColor(global::Android.Graphics.Color.Transparent);
        }
    }
}