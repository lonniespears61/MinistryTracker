// AppShell.cs — defines Shell tabs and app routes — 2026-01-19

using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.Views;

namespace MinistryTracker;

public partial class AppShell : Shell
{
    public const string DashboardTabRoute = "DashboardTab";
    public const string CalendarTabRoute = "CalendarTab";

    private readonly IServiceProvider _services;

    public AppShell(IServiceProvider services)
    {
        InitializeComponent();

        _services = services;

        RegisterRoutes();   // ✅ Keep routes registered before navigation happens
        BuildTabs();        // ✅ Build TabBar in code using DI pages
    }

    private void BuildTabs()
    {
        // Clear anything that might have been added by XAML (future-proofing)
        Items.Clear();

        var tabs = new TabBar();

        tabs.Items.Add(new ShellContent
        {
            Route = DashboardTabRoute,
            Title = "Dashboard",
            Icon = "icon_home.png",
            ContentTemplate = new DataTemplate(() => _services.GetRequiredService<DashboardPage>())
        });

        tabs.Items.Add(new ShellContent
        {
            Title = "Students",
            Icon = "icon_students.png",
            ContentTemplate = new DataTemplate(() => _services.GetRequiredService<StudentsListPage>())
        });

        // ✅ Settings removed from TabBar (will be accessed via header icon)
        // tabs.Items.Add(new ShellContent
        // {
        //     Title = "Settings",
        //     Icon = "icon_settings.png",
        //     ContentTemplate = new DataTemplate(() => _services.GetRequiredService<SettingsPage>())
        // });

        tabs.Items.Add(new ShellContent
        {
            Route = CalendarTabRoute,
            Title = "My Calendar",
            Icon = "icon_calendar.png",
            ContentTemplate = new DataTemplate(() => _services.GetRequiredService<MyCalendarPage>())
        });

        Items.Add(tabs);
    }

    private static void RegisterRoutes()
    {
        // Student-related pages
        Routing.RegisterRoute(nameof(StudentProfilePage), typeof(StudentProfilePage));
        Routing.RegisterRoute(nameof(StudentVisitHistoryPage), typeof(StudentVisitHistoryPage));
        Routing.RegisterRoute(nameof(EditStudentPage), typeof(EditStudentPage));
        Routing.RegisterRoute(nameof(AddStudentPage), typeof(AddStudentPage));
        Routing.RegisterRoute(nameof(StudentsMapPage), typeof(StudentsMapPage));
        Routing.RegisterRoute(nameof(MyCalendarPage), typeof(MyCalendarPage));
        Routing.RegisterRoute(nameof(SelectStudentForVisitPage), typeof(SelectStudentForVisitPage));


        // Visit-related pages
        Routing.RegisterRoute(nameof(AddVisitPage), typeof(AddVisitPage));
        Routing.RegisterRoute(nameof(UpdateVisitPage), typeof(UpdateVisitPage));

        //Settings Page
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(BackupExportPage), typeof(BackupExportPage));
        Routing.RegisterRoute(nameof(FeedbackPage), typeof(FeedbackPage));
    }
}
