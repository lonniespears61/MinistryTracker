using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MinistryTracker
{
    [Activity(Theme = "@style/Maui.SplashTheme",
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

            // -----------------------------------------------------------------------------------------------------------------
            // EDGE-TO-EDGE HANDLING (ANDROID API SAFE)
            //
            // RULES:
            // - API < 30 → method does NOT exist
            // - API 30–34 → safe to use
            // - API 35+ → obsolete (Android 15 forces edge-to-edge automatically)
            // -----------------------------------------------------------------------------------------------------------------

            if (OperatingSystem.IsAndroidVersionAtLeast(30) &&
                !OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                Window?.SetDecorFitsSystemWindows(false);
            }

            // -----------------------------------------------------------------------------------------------------------------
            // STATUS BAR COLOR
            //
            // - API < 21 → not supported
            // - API 21–34 → safe
            // - API 35+ → obsolete (system handles it)
            // -----------------------------------------------------------------------------------------------------------------

            if (OperatingSystem.IsAndroidVersionAtLeast(21) &&
                !OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                Window?.SetStatusBarColor(Android.Graphics.Color.Transparent);
            }
        }
    }
}