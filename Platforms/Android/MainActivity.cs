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

        // Edge-to-edge is only supported on Android 11 / API 30+
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            Window.SetDecorFitsSystemWindows(false);
        }

        // SetStatusBarColor is obsolete on newer Android versions, so only use it below API 35
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop &&
            Build.VERSION.SdkInt < BuildVersionCodes.VanillaIceCream)
        {
            Window.SetStatusBarColor(global::Android.Graphics.Color.Transparent);
        }
    }
}