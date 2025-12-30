using MinistryTracker.Views;

namespace MinistryTracker;

public partial class AppShell : Shell
{
    private readonly IServiceProvider _services;

    public AppShell(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;

        BuildTabs();
    }

    private void BuildTabs()
    {
        // Clear anything that might have been added by XAML (future-proofing)
        Items.Clear();

        var tabs = new TabBar();

        tabs.Items.Add(new ShellContent
        {
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

        tabs.Items.Add(new ShellContent
        {
            Title = "Settings",
            Icon = "icon_settings.png",
            ContentTemplate = new DataTemplate(() => _services.GetRequiredService<SettingsPage>())
        });

        tabs.Items.Add(new ShellContent
        {
            Title = "MyCalendar",
            Icon = "icon_calendar.png",
            ContentTemplate = new DataTemplate(() => _services.GetRequiredService<MyCalendarPage>())
        });

        Items.Add(tabs);
    }
}
