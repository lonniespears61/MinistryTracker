// ---------------------------------------------------------------------------------------------------------------------
// MyCalendarPage.xaml.cs — My calendar page shell host — 2026-02-01
// Purpose: Loads VM, receives schedule context from Shell query params,
//          and performs navigation when VM raises ScheduleVisitRequested.
// ---------------------------------------------------------------------------------------------------------------------

using System;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

[QueryProperty(nameof(Mode), "mode")]
[QueryProperty(nameof(StudentId), "studentId")]
public partial class MyCalendarPage : ContentPage
{
    private readonly MyCalendarViewModel _vm;

    private bool _loading;
    private bool _subscribed;

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

        if (!_subscribed)
        {
            _vm.ScheduleVisitRequested += OnScheduleVisitRequested;
            _subscribed = true;
        }

        _ = TryLoadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

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

            await _vm.LoadAsync();

            if (string.Equals(Mode, "schedule", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(StudentId, out var sid) &&
                sid > 0)
            {
                await _vm.BeginSchedulingForStudentAsync(sid);
            }
            else
            {
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
            var dateString = date.ToString("yyyy-MM-dd");
            await Shell.Current.GoToAsync($"{nameof(AddVisitPage)}?studentId={studentId}&date={dateString}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlert("Something Broke", "Back up and try again", "OK");
        }
    }
}
