// ---------------------------------------------------------------------------------------------------------------------
// MyCalendarPage.xaml.cs — My calendar page shell host — 2026-01-30
// Purpose: Binds VM, loads visits, receives "schedule mode" from Shell query params,
//          and performs navigation when VM requests scheduling.
// ---------------------------------------------------------------------------------------------------------------------

using System;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(StudentId), "studentId")]
public partial class MyCalendarPage : ContentPage
{
    private readonly MyCalendarViewModel _vm;

    // Simple re-entrancy guard for OnAppearing (Shell can trigger multiple times)
    private bool _loading;

    // Prevent double-subscribe (Shell can recreate/rehydrate pages depending on navigation)
    private bool _subscribed;

    // Shell query params arrive as strings; Shell sets these BEFORE OnAppearing.
    public string? Mode { get; set; }
    public string? StudentId { get; set; }

    public MyCalendarPage(MyCalendarViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        BindingContext = _vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe once per page lifetime
        if (!_subscribed)
        {
            _vm.ScheduleVisitRequested += OnScheduleVisitRequested;
            _subscribed = true;
        }

        // Kick async load without blocking OnAppearing
        _ = TryLoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe to prevent memory leaks / duplicate navigation
        if (_subscribed)
        {
            _vm.ScheduleVisitRequested -= OnScheduleVisitRequested;
            _subscribed = false;
        }
    }

    private async System.Threading.Tasks.Task TryLoadAsync()
    {
        if (_loading) return;

        try
        {
            _loading = true;

            // 1) Load calendar data (future visits etc.)
            await _vm.LoadAsync();

            // 2) If we arrived from StudentsList in "schedule" mode, hand off context to VM
            if (string.Equals(Mode, "schedule", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(StudentId, out var sid) &&
                sid > 0)
            {
                _vm.BeginSchedulingForStudent(sid);
            }
            else
            {
                // Opened normally from tab: clear any old scheduling context
                _vm.ClearSchedulingContext();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally
        {
            _loading = false;
        }
    }

    private async void OnScheduleVisitRequested(int studentId, DateTime date)
    {
        try
        {
            // Date-only in a stable format; AddVisitPage will ask for time/place.
            var dateString = date.ToString("yyyy-MM-dd");

            await Shell.Current.GoToAsync(
                $"{nameof(AddVisitPage)}?studentId={studentId}&date={dateString}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlert("Something Broke", "Back up and try again", "OK");
        }
    }
}
