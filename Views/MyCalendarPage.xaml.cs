// ---------------------------------------------------------------------------------------------------------------------
// MyCalendarPage.xaml.cs (DROP-IN - reviewed)
// ---------------------------------------------------------------------------------------------------------------------

using System;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

public partial class MyCalendarPage : ContentPage
{
    private readonly MyCalendarViewModel _vm;

    // Simple re-entrancy guard for OnAppearing (Shell can trigger multiple times)
    private bool _loading;

    public MyCalendarPage(MyCalendarViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Prevent overlapping loads (double taps on tabs, fast navigation, etc.)
        if (_loading) return;

        try
        {
            _loading = true;
            await _vm.LoadAsync();
        }
        catch (Exception ex)
        {
            // Keep it quiet for now (your support system comes later).
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            _loading = false;
        }
    }
}
