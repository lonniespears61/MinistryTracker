using MinistryTracker.Data;
using MinistryTracker.Views;

namespace MinistryTracker;

public partial class App : Application
{
    public App(DashboardPage root, DataService data)
    {
        InitializeComponent();

        // Kick off DB init (non-blocking; service itself is concurrency-safe)
        _ = data.InitializeAsync();

        MainPage = new NavigationPage(root);
    }
}
