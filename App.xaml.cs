using Microsoft.Extensions.DependencyInjection; 
using MinistryTracker.Data;
using MinistryTracker.Views;

namespace MinistryTracker;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
       
        var dashboardPage = MauiProgram.Services.GetRequiredService<DashboardPage>();
        return new Window(new NavigationPage(dashboardPage));
    }
}
