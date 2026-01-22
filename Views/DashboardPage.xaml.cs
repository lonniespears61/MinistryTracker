// DashboardPage.xaml.cs — Dashboard interactions — 2026-01-21

using System;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            // Keep dashboard resilient; no modal spam during ministry.
        }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        // NOTE: This requires SettingsPage to be reachable via:
        // - a tab, OR
        // - a registered Shell route (recommended).
        try
        {
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }
        catch
        {
            // If route isn't registered, this will fail.
            // Next session: set ShellContent.Route for tabs OR register SettingsPage route.
        }
    }

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(AboutPage));
        }
        catch
        {
            // Same note as Settings: register route or expose via tab.
        }
    }

    private async void OnAddNewCallClicked(object sender, EventArgs e)
    {
        // This is the “walk away from the door” action:
        // Add new call with notes + capture WHERE (handled on Add page).
        await Shell.Current.GoToAsync(nameof(AddStudentPage));
    }

    private async void OnOpenMapClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(StudentsMapPage));
    }

    private async void OnViewCallsClicked(object sender, EventArgs e)
    {
        // If Students/Calls is a tab, best practice is to switch to it using a known route.
        // Next session: set Route="calls" (or "students") on that ShellContent.
        // For now we try route name first; if it fails, do nothing.
        try
        {
            await Shell.Current.GoToAsync("//Students");
        }
        catch
        {
            // Next session fix: define stable tab routes in AppShell.BuildTabs().
        }
    }
}
