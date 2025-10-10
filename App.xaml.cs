using Microsoft.Maui; // for Window
using MinistryTracker.Views;

namespace MinistryTracker;

public partial class App : Application
{
    private readonly DashboardPage _dashboard;

    // MAUI will resolve DashboardPage from DI and pass it in.
    public App(DashboardPage dashboard)
    {
        InitializeComponent();
        _dashboard = dashboard;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(_dashboard));
    }
}
