// ---------------------------------------------------------------------------------------------------------------------
// MyCalendarViewModel.cs — Month calendar VM — 2026-02-01
// Purpose: Month grid + agenda + optional "schedule mode" when launched from a student.
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

    // -------------------------
    // Schedule Mode (context)
    // -------------------------

    [ObservableProperty] private bool isSchedulingMode;
    [ObservableProperty] private int? schedulingStudentId;
    [ObservableProperty] private string schedulingStudentName = string.Empty;

    public string SchedulingBannerText =>
        IsSchedulingMode && SchedulingStudentId is not null
            ? $"Scheduling a new visit for: {SchedulingStudentName}"
            : string.Empty;

    public event Action<int?, DateTime>? ScheduleVisitRequested;

    // -------------------------
    // Calendar state
    // -------------------------

    [ObservableProperty] private DateTime displayedMonth; // normalized to the 1st
    [ObservableProperty] private bool isBusy;

    public ObservableCollection<CalendarDayCellViewModel> DayCells { get; } = new();

    [ObservableProperty] private CalendarDayCellViewModel? selectedDayCell;

    public ObservableCollection<VisitWithStudent> SelectedDayVisits { get; } = new();

    public string MonthTitle => DisplayedMonth.ToString("MMMM yyyy");

    public string SelectedDayTitle =>
        SelectedDayCell is null || SelectedDayCell.IsPlaceholder
            ? "Select a day"
            : SelectedDayCell.Date.ToString("dddd, MMMM d");

    public bool ShowTodayButton =>
        DisplayedMonth.Year != DateTime.Today.Year || DisplayedMonth.Month != DateTime.Today.Month;

    public bool CanAddVisitForSelectedDay =>
        SelectedDayCell is not null &&
        !SelectedDayCell.IsPlaceholder &&
        SelectedDayCell.Date.Date >= DateTime.Today &&
        (!IsSchedulingMode || SchedulingStudentId is not null);

    public MyCalendarViewModel(DataService data, ILogger<MyCalendarViewModel>? log = null)
    {
        _data = data;
        _log = log;

        DisplayedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    // -------------------------
    // Public Load
    // -------------------------

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            _allFutureVisits = await _data.GetFutureVisitsWithStudentsAsync().ConfigureAwait(false);

            BuildVisitsLookup(_allFutureVisits);

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

    public async Task BeginSchedulingForStudentAsync(int studentId)
    {
        try
        {
            var student = await _data.GetStudentByIdAsync(studentId).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsSchedulingMode = true;
                SchedulingStudentId = studentId;
                SchedulingStudentName = student?.Name ?? $"Student #{studentId}";

                OnPropertyChanged(nameof(SchedulingBannerText));
                OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
            });
        }
        catch
        {
            // If lookup fails, still allow scheduling, just without a name.
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsSchedulingMode = true;
                SchedulingStudentId = studentId;
                SchedulingStudentName = $"Student #{studentId}";

                OnPropertyChanged(nameof(SchedulingBannerText));
                OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
            });
        }
    }

    public void ClearSchedulingContext()
    {
        IsSchedulingMode = false;
        SchedulingStudentId = null;
        SchedulingStudentName = string.Empty;

        OnPropertyChanged(nameof(SchedulingBannerText));
        OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
    }

    // -------------------------
    // Commands
    // -------------------------

    [RelayCommand] private async Task Refresh() => await LoadAsync();

    [RelayCommand] private void PrevMonth() => SetDisplayedMonth(DisplayedMonth.AddMonths(-1));
    [RelayCommand] private void NextMonth() => SetDisplayedMonth(DisplayedMonth.AddMonths(1));
    [RelayCommand] private void GoToToday() => SetDisplayedMonth(DateTime.Today);

    [RelayCommand]
    private void AddVisitForSelectedDay()
    {
        if (!CanAddVisitForSelectedDay) return;
        if (SelectedDayCell is null || SelectedDayCell.IsPlaceholder) return;

        ScheduleVisitRequested?.Invoke(SchedulingStudentId, SelectedDayCell.Date.Date);
    }

    // -------------------------
    // Selection changed
    // -------------------------

    partial void OnSelectedDayCellChanged(CalendarDayCellViewModel? value)
    {
        foreach (var cell in DayCells)
            cell.IsSelected = false;

        if (value is not null && !value.IsPlaceholder)
            value.IsSelected = true;

        SyncAgendaForSelection();

        OnPropertyChanged(nameof(SelectedDayTitle));
        OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
    }

    // -------------------------
    // Internals
    // -------------------------

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

        var leadingPlaceholders = (int)first.DayOfWeek; // Sunday-first

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

        while (DayCells.Count % 7 != 0)
            DayCells.Add(CalendarDayCellViewModel.Placeholder());

        if (DisplayedMonth.Year == DateTime.Today.Year && DisplayedMonth.Month == DateTime.Today.Month)
            SelectedDayCell = DayCells.FirstOrDefault(c => !c.IsPlaceholder && c.Date == DateTime.Today);
        else if (SelectedDayCell is null || SelectedDayCell.IsPlaceholder)
            SelectedDayCell = null;
    }

    private void SetDisplayedMonth(DateTime anyDateInMonth)
    {
        DisplayedMonth = new DateTime(anyDateInMonth.Year, anyDateInMonth.Month, 1);

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
        OnPropertyChanged(nameof(SchedulingBannerText));
    }
}
