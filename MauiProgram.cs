using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinistryTracker.Data;
using MinistryTracker.ViewModels;
using MinistryTracker.Views;
using Microsoft.Maui.Controls.Maps;



namespace MinistryTracker;

public static class MauiProgram
{
    // Optional global escape hatch; prefer constructor DI when possible.
    public static IServiceProvider Services { get; private set; } = default!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
           
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug(); // helpful during emulator/device testing
#endif

        // ===== Services =====
        builder.Services.AddSingleton<DataService>(); // SQLite wrapper / repo
        builder.Services.AddSingleton<AppShell>();    // ✅ Shell root for DI

        // ===== ViewModels =====
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<StudentsListViewModel>();
        builder.Services.AddTransient<AddStudentViewModel>();
        builder.Services.AddTransient<EditStudentViewModel>();
        builder.Services.AddTransient<AddVisitViewModel>();
        builder.Services.AddTransient<StudentProfileViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
     
        builder.Services.AddTransient<MyCalendarViewModel>();
        builder.Services.AddTransient<UpdateVisitViewModel>();
        builder.Services.AddTransient<StudentsMapViewModel>();

        // ===== Pages =====
        // Register every page you navigate to so DI can supply their VMs/services.
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<StudentsListPage>();
        builder.Services.AddTransient<AddStudentPage>();
        builder.Services.AddTransient<EditStudentPage>();
        builder.Services.AddTransient<AddVisitPage>();
        builder.Services.AddTransient<StudentProfilePage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<MyCalendarPage>();
        builder.Services.AddTransient<UpdateVisitPage>();
        builder.Services.AddTransient<StudentsMapPage>();

        // ✅ Build first; only then use Services
        var app = builder.Build();

        // ✅ Make provider available (avoid using this inside constructors if possible)
        Services = app.Services;

        // ✅ Fire-and-forget startup work (non-blocking)
        _ = InitializeAsync(app.Services);

        return app;
    }

    /// <summary>
    /// Perform non-blocking startup tasks (DB warm-up, one-time migrations, etc.).
    /// Never block the UI thread here.
    /// </summary>
    private static async Task InitializeAsync(IServiceProvider services)
    {
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("Startup");

        try
        {
            var dataService = services.GetRequiredService<DataService>();
            await dataService.InitializeAsync().ConfigureAwait(false);
            //   await dataService.SeedVisitsAsync().ConfigureAwait(false)   ;   // <-- removed to avoid auto-seeding on startup

            logger?.LogInformation("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            // Log and continue; surface a toast/dialog later if desired.
            logger?.LogError(ex, "Startup initialization failed.");
        }
    }
}
