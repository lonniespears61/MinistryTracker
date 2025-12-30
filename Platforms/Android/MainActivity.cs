using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MinistryTracker
{
    [Activity(Theme = "@style/Maui.SplashTheme",
        MainLauncher = true, 
        LaunchMode = LaunchMode.SingleTop, 
        ConfigurationChanges = ConfigChanges.ScreenSize | 
        ConfigChanges.Orientation | ConfigChanges.UiMode | 
        ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // ✅ Allows your UI to draw behind the system status bar (edge-to-edge)
            Window?.SetDecorFitsSystemWindows(false);

            // ✅ Optional: make the status bar transparent so your page background shows through
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window?.SetStatusBarColor(Android.Graphics.Color.Transparent);
            }
        }
    }
}
