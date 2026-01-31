// ---------------------------------------------------------------------------------------------------------------------
// MyCalendarViewModel.cs — Month calendar + agenda VM — 2026-01-30
//
// PURPOSE:
// - Month-grid calendar (Sunday-first)
// - Loads FUTURE visits across all students via DataService.GetFutureVisitsWithStudentsAsync()
// - Builds DayCells incl. placeholder padding cells
// - Shows selected-day agenda list below the grid
//
// STANDARDS:
// ✅ No Shell navigation in VM (Page handles navigation)
// ✅ One DB call for visits
// ✅ All ObservableCollection mutations happen on UI thread
// ✅ Small dataset => in-memory grouping is fine
//
// SCHEDULING MODE:
// - When opened from StudentsList swipe ("Schedule Visit"), the Page passes a studentId.
// - VM stores PendingStudentId and raises ScheduleVisitRequested when user confirms a date.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.Logging;
using MinistryTracker.Data;
using MinistryTracker.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels;

public partial class MyCalendarViewModel : ObservableObject
{
    private readonly DataService _data;
    private readonly ILogger<MyCalendarViewModel>? _log;

    private List<VisitWithStudent> _allFutureVisits = new();
    private readonly Dictionary<DateTime, List<VisitWithStudent>> _visitsByDate = new();

    [ObservableProperty]
    private DateTime displayedMonth; // normalized to the 1st

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<CalendarDayCellViewModel> DayCells { get; } = new();

    [ObservableProperty]
    private CalendarDayCellViewModel? selectedDayCell;

    public ObservableCollection<VisitWithStudent> SelectedDayVisits { get; } = new();

    // -------------------------------------------------------------------------------------------------------------
    // Scheduling context (optional)
    // -------------------------------------------------------------------------------------------------------------

    // When set, the calendar is being used to pick a date for a specific student.
    [ObservableProperty]
    private int? pendingStudentId;

    public bool IsSchedulingMode => PendingStudentId.HasValue;

    // Page subscribes to this and performs navigation (keeps "no Shell nav in VM" rule).
    public event Action<int, DateTime>? ScheduleVisitRequested;

    // -------------------------------------------------------------------------------------------------------------

    public string MonthTitle => DisplayedMonth.ToString("MMMM yyyy");

    public string SelectedDayTitle =>
        SelectedDayCell is null || SelectedDayCell.IsPlaceholder
            ? "Select a day"
            : SelectedDayCell.Date.ToString("dddd, MMMM d");

    public bool ShowTodayButton =>
        DisplayedMonth.Year != DateTime.Today.Year || DisplayedMonth.Month != DateTime.Today.Month;

    public bool CanAddVisitForSelectedDay =>
        SelectedDayCell is not null && !SelectedDayCell.IsPlaceholder;

    public MyCalendarViewModel(DataService data, ILogger<MyCalendarViewModel>? log = null)
    {
        _data = data;
        _log = log;

        DisplayedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // PUBLIC LOAD
    // -----------------------------------------------------------------------------------------------------------------

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // DB call off UI thread is fine.
            _allFutureVisits = await _data.GetFutureVisitsWithStudentsAsync().ConfigureAwait(false);

            BuildVisitsLookup(_allFutureVisits);

            // UI updates MUST happen on UI thread.
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                BuildMonthGrid();
                SyncAgendaForSelection();
                RaiseHeaderProps();
            });
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "Failed to load MyCalendar data.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    // -----------------------------------------------------------------------------------------------------------------
    // MONTH NAVIGATION
    // -----------------------------------------------------------------------------------------------------------------

    [RelayCommand]
    private void PrevMonth() => SetDisplayedMonth(DisplayedMonth.AddMonths(-1));

    [RelayCommand]
    private void NextMonth() => SetDisplayedMonth(DisplayedMonth.AddMonths(1));

    [RelayCommand]
    private void GoToToday() => SetDisplayedMonth(DateTime.Today);

    private void SetDisplayedMonth(DateTime anyDateInMonth)
    {
        // Normalize to the 1st of that month
        DisplayedMonth = new DateTime(anyDateInMonth.Year, anyDateInMonth.Month, 1);

        // UI collections should be touched on UI thread.
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                BuildMonthGrid();
                SyncAgendaForSelection();
                RaiseHeaderProps();
            });
            return;
        }

        BuildMonthGrid();
        SyncAgendaForSelection();
        RaiseHeaderProps();
    }

    // -----------------------------------------------------------------------------------------------------------------
    // SCHEDULING MODE ENTRY (called by Page when navigated with studentId)
    // -----------------------------------------------------------------------------------------------------------------

    public void BeginSchedulingForStudent(int studentId)
    {
        PendingStudentId = studentId > 0 ? studentId : null;
        OnPropertyChanged(nameof(IsSchedulingMode));
    }

    public void ClearSchedulingContext()
    {
        PendingStudentId = null;
        OnPropertyChanged(nameof(IsSchedulingMode));
    }

    // Optional placeholder command (no nav in VM).
    // Page subscribes to ScheduleVisitRequested and navigates accordingly.
    [RelayCommand]
    private void AddVisitForSelectedDay()
    {
        if (PendingStudentId is null) return;
        if (SelectedDayCell is null || SelectedDayCell.IsPlaceholder) return;

        // Date-only; AddVisitPage will ask for time/place.
        ScheduleVisitRequested?.Invoke(PendingStudentId.Value, SelectedDayCell.Date.Date);
    }

    // -----------------------------------------------------------------------------------------------------------------
    // SELECTION CHANGED
    // -----------------------------------------------------------------------------------------------------------------

    partial void OnSelectedDayCellChanged(CalendarDayCellViewModel? value)
    {
        // Toggle selection visuals.
        foreach (var cell in DayCells)
            cell.IsSelected = false;

        if (value is not null && !value.IsPlaceholder)
            value.IsSelected = true;

        SyncAgendaForSelection();

        OnPropertyChanged(nameof(SelectedDayTitle));
        OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
    }

    // -----------------------------------------------------------------------------------------------------------------
    // INTERNAL HELPERS
    // -----------------------------------------------------------------------------------------------------------------

    private void BuildVisitsLookup(List<VisitWithStudent> rows)
    {
        _visitsByDate.Clear();

        foreach (var v in rows)
        {
            var key = v.ScheduledDateTime.Date;

            if (!_visitsByDate.TryGetValue(key, out var list))
            {
                list = new List<VisitWithStudent>();
                _visitsByDate[key] = list;
            }

            list.Add(v);
        }

        foreach (var kv in _visitsByDate)
            kv.Value.Sort((a, b) => a.ScheduledDateTime.CompareTo(b.ScheduledDateTime));
    }

    private void BuildMonthGrid()
    {
        DayCells.Clear();

        var first = new DateTime(DisplayedMonth.Year, DisplayedMonth.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(first.Year, first.Month);

        // Sunday-first offset: Sunday=0..Saturday=6
        var leadingPlaceholders = (int)first.DayOfWeek;

        for (int i = 0; i < leadingPlaceholders; i++)
            DayCells.Add(CalendarDayCellViewModel.Placeholder());

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(first.Year, first.Month, day);
            var cell = new CalendarDayCellViewModel(date);

            if (_visitsByDate.TryGetValue(date.Date, out var list))
                cell.VisitCount = list.Count;

            DayCells.Add(cell);
        }

        // Fill last row to full week for nicer grid symmetry
        while (DayCells.Count % 7 != 0)
            DayCells.Add(CalendarDayCellViewModel.Placeholder());

        // Preserve selection if still visible in this month
        if (SelectedDayCell is not null && !SelectedDayCell.IsPlaceholder)
        {
            var oldDate = SelectedDayCell.Date;
            var match = DayCells.FirstOrDefault(c => !c.IsPlaceholder && c.Date == oldDate);
            if (match is not null)
            {
                SelectedDayCell = match;
                return;
            }
        }

        // Default selection: select today if current month
        if (DisplayedMonth.Year == DateTime.Today.Year && DisplayedMonth.Month == DateTime.Today.Month)
            SelectTodayIfVisible();
        else
            SelectedDayCell = null;
    }

    private void SelectTodayIfVisible()
    {
        var today = DateTime.Today;
        SelectedDayCell = DayCells.FirstOrDefault(c => !c.IsPlaceholder && c.Date == today);
    }

    private void SyncAgendaForSelection()
    {
        SelectedDayVisits.Clear();

        if (SelectedDayCell is null || SelectedDayCell.IsPlaceholder)
        {
            OnPropertyChanged(nameof(SelectedDayTitle));
            return;
        }

        var key = SelectedDayCell.Date.Date;

        if (_visitsByDate.TryGetValue(key, out var list))

        {
            foreach (var v in list)
                SelectedDayVisits.Add(v);
        }

        OnPropertyChanged(nameof(SelectedDayTitle));
    }

    private void RaiseHeaderProps()
    {
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(ShowTodayButton));
        OnPropertyChanged(nameof(SelectedDayTitle));
        OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
        OnPropertyChanged(nameof(IsSchedulingMode));
    }
}
