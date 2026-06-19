// ---------------------------------------------------------------------------------------------------------------------
// MyCalendarViewModel.cs — Month calendar VM — 2026-02-01
// Purpose: Month grid + agenda + optional "schedule mode" when launched from a student.
//
// CHANGE NOTES
// - In scheduling mode, tapping a valid day immediately continues the schedule flow.
// - Added agenda-item tap handling so the page can decide whether to:
//   * schedule the current student on that date
//   * open the tapped visit
//   * or cancel
// - Added reschedule mode so Calendar can be reused as a date chooser for an
//   existing visit that should be moved without editing the original record in place.
//
// DESIGN NOTES
// - The ViewModel raises intent events.
// - The page owns prompts / alerts / navigation decisions.
// - This keeps user-choice UX in the View and business state in the ViewModel.
// - Schedule mode and reschedule mode intentionally share the same calendar UX;
//   the difference is which event is raised when a valid date is chosen.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.Logging;
using MinistryTracker.Data;
using MinistryTracker.Data.Repositories;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels;

public partial class MyCalendarViewModel : ObservableObject
{
    private readonly IStudentRepository _students;
    private readonly IVisitRepository _visits;
    private readonly ILogger<MyCalendarViewModel>? _log;

    private List<VisitWithStudent> _visibleMonthVisits = new();
    private readonly Dictionary<DateTime, List<VisitWithStudent>> _visitsByDate = new();

    // -------------------------
    // Schedule / Reschedule Mode (context)
    // -------------------------

    [ObservableProperty] private bool isSchedulingMode;
    [ObservableProperty] private int? schedulingStudentId;
    [ObservableProperty] private string schedulingStudentName = string.Empty;

    [ObservableProperty] private bool isRescheduleMode;
    [ObservableProperty] private int? reschedulingVisitId;
    [ObservableProperty] private TimeSpan rescheduleTime;
    [ObservableProperty] private string? rescheduleReason;

    public string SchedulingBannerText
    {
        get
        {
            if (!IsSchedulingMode || SchedulingStudentId is null)
                return string.Empty;

            return IsRescheduleMode && ReschedulingVisitId is not null
                ? $"Rescheduling visit for: {SchedulingStudentName}"
                : $"Scheduling a new visit for: {SchedulingStudentName}";
        }
    }

    /// <summary>
    /// Raised when the user has chosen a date for a NEW visit.
    /// The page responds by navigating to AddVisitPage.
    /// </summary>
    public event Action<int?, DateTime>? ScheduleVisitRequested;

    /// <summary>
    /// Raised when the user has chosen a date for a RESCHEDULE.
    /// The page responds by calling the reschedule workflow for the existing visit.
    /// </summary>
    public event Action<int, int, DateTime>? RescheduleVisitRequested;

    /// <summary>
    /// Raised when the user taps an existing visit in the agenda list.
    /// The page responds by deciding whether to prompt, open edit flow,
    /// or convert the tap into "schedule current student on this date".
    /// </summary>
    public event Action<VisitWithStudent>? ExistingVisitTapped;

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

    public string SelectedDayActionText => IsRescheduleMode ? "Reschedule" : "Add";

    public MyCalendarViewModel(
        IStudentRepository students,
        IVisitRepository visits,
        ILogger<MyCalendarViewModel>? log = null)
    {
        _students = students;
        _visits = visits;
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

            var monthStart = DisplayedMonth.Date;
            var nextMonthStart = monthStart.AddMonths(1);

            var monthVisits = await _visits
                .GetVisitsWithStudentsInRangeAsync(monthStart, nextMonthStart, includeCanceled: false)
                .ConfigureAwait(false);

            _visibleMonthVisits = monthVisits
                .Where(v => v.Status != VisitStatus.Rescheduled)
                .ToList();

            BuildVisitsLookup(_visibleMonthVisits);

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
            var student = await _students.GetStudentByIdAsync(studentId).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsRescheduleMode = false;
                ReschedulingVisitId = null;

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
                IsRescheduleMode = false;
                ReschedulingVisitId = null;

                IsSchedulingMode = true;
                SchedulingStudentId = studentId;
                SchedulingStudentName = $"Student #{studentId}";

                OnPropertyChanged(nameof(SchedulingBannerText));
                OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
            });
        }
    }

    /// <summary>
    /// Starts Calendar in reschedule mode for an existing visit.
    ///
    /// WHY:
    /// Reschedule is not an in-place datetime edit. Calendar acts as the date
    /// chooser, but the page layer will ultimately call RescheduleVisitAsync
    /// using the original visit id and the newly chosen date.
    /// </summary>
    public async Task BeginRescheduleAsync(int visitId, int studentId)
    {
        try
        {
            var visit = await _visits.GetVisitByIdAsync(visitId).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Visit not found.");

            var actualStudentId = visit.StudentId;
            var student = await _students.GetStudentByIdAsync(actualStudentId).ConfigureAwait(false);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsRescheduleMode = true;
                ReschedulingVisitId = visitId;
                RescheduleTime = visit.ScheduledDateTime.TimeOfDay;
                RescheduleReason = null;

                IsSchedulingMode = true;
                SchedulingStudentId = actualStudentId;
                SchedulingStudentName = student?.Name ?? $"Student #{actualStudentId}";

                OnPropertyChanged(nameof(SchedulingBannerText));
                OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
                OnPropertyChanged(nameof(SelectedDayActionText));
            });
        }
        catch
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                IsRescheduleMode = true;
                ReschedulingVisitId = visitId;
                RescheduleTime = DateTime.Now.TimeOfDay;
                RescheduleReason = null;

                IsSchedulingMode = true;
                SchedulingStudentId = studentId;
                SchedulingStudentName = $"Student #{studentId}";

                OnPropertyChanged(nameof(SchedulingBannerText));
                OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
                OnPropertyChanged(nameof(SelectedDayActionText));
            });
        }
    }

    public void ClearSchedulingContext()
    {
        IsRescheduleMode = false;
        ReschedulingVisitId = null;
        RescheduleTime = default;
        RescheduleReason = null;

        IsSchedulingMode = false;
        SchedulingStudentId = null;
        SchedulingStudentName = string.Empty;

        OnPropertyChanged(nameof(SchedulingBannerText));
        OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
        OnPropertyChanged(nameof(SelectedDayActionText));
    }

    /// <summary>
    /// Clears only the transient day selection.
    ///
    /// WHY:
    /// If the user backs out of Add Visit, we want to keep the student scheduling
    /// context but release the date choice so they can pick another day cleanly.
    ///
    /// This also applies to reschedule mode:
    /// keep the visit/student context, release the day choice.
    /// </summary>
    public void ClearSelectedDay()
    {
        SelectedDayCell = null;
        SyncAgendaForSelection();
        OnPropertyChanged(nameof(SelectedDayTitle));
        OnPropertyChanged(nameof(CanAddVisitForSelectedDay));
    }

    public async Task CompleteRescheduleAsync(DateTime newDate)
    {
        if (!IsRescheduleMode || ReschedulingVisitId is null)
            throw new InvalidOperationException("No visit is selected for rescheduling.");

        if (newDate.Date < DateTime.Today)
            throw new InvalidOperationException("A visit cannot be rescheduled before today.");

        var visitId = ReschedulingVisitId.Value;
        var original = await _visits.GetVisitByIdAsync(visitId).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Visit not found.");

        var replacementDateTime = newDate.Date.Add(RescheduleTime);

        var reason = string.IsNullOrWhiteSpace(RescheduleReason)
            ? null
            : $"Rescheduled: {RescheduleReason.Trim()}";

        await _visits
            .RescheduleVisitAsync(visitId, replacementDateTime, note: reason)
            .ConfigureAwait(false);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ClearSchedulingContext();
            DisplayedMonth = new DateTime(
                replacementDateTime.Year,
                replacementDateTime.Month,
                1);
        });

        await LoadAsync().ConfigureAwait(false);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SelectedDayCell = DayCells.FirstOrDefault(
                cell => !cell.IsPlaceholder && cell.Date.Date == replacementDateTime.Date);
        });
    }

    // -------------------------
    // Commands
    // -------------------------

    [RelayCommand] private async Task Refresh() => await LoadAsync();

    [RelayCommand] private async Task PrevMonth() => await SetDisplayedMonthAsync(DisplayedMonth.AddMonths(-1));
    [RelayCommand] private async Task NextMonth() => await SetDisplayedMonthAsync(DisplayedMonth.AddMonths(1));
    [RelayCommand] private async Task GoToToday() => await SetDisplayedMonthAsync(DateTime.Today);

    [RelayCommand]
    private void AddVisitForSelectedDay()
    {
        if (!CanAddVisitForSelectedDay) return;
        if (SelectedDayCell is null || SelectedDayCell.IsPlaceholder) return;
        if (SelectedDayCell.Date.Date < DateTime.Today) return;

        if (IsRescheduleMode && ReschedulingVisitId is not null)
        {
            if (SchedulingStudentId is null) return;

            RescheduleVisitRequested?.Invoke(
                ReschedulingVisitId.Value,
                SchedulingStudentId.Value,
                SelectedDayCell.Date.Date);
        }
        else
        {
            ScheduleVisitRequested?.Invoke(
                SchedulingStudentId,
                SelectedDayCell.Date.Date);
        }
    }

    [RelayCommand]
    private void OpenExistingVisit(VisitWithStudent? visit)
    {
        if (visit is null)
            return;

        // A student/visit is already pending in Calendar. Existing agenda
        // entries remain visible for context, but cannot replace that workflow.
        if (IsSchedulingMode && SchedulingStudentId is not null)
            return;

        ExistingVisitTapped?.Invoke(visit);
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

        // ---------------------------------------------------------------------
        // SCHEDULING TAP BEHAVIOR
        //
        // WHY:
        // In normal calendar mode, tapping a day should only select it and show
        // that day's agenda.
        //
        // In scheduling mode, the user already came from a known student and is
        // using the calendar to answer: "What day works?"
        //
        // Reschedule mode stops after selecting the day so the user can choose
        // the replacement time and enter a reason before confirming.
        // ---------------------------------------------------------------------
        if (IsSchedulingMode &&
            SchedulingStudentId is not null &&
            value is not null &&
            !value.IsPlaceholder &&
            value.Date.Date >= DateTime.Today)
        {
            if (!IsRescheduleMode)
            {
                ScheduleVisitRequested?.Invoke(
                    SchedulingStudentId.Value,
                    value.Date.Date);
            }
        }
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

    private async Task SetDisplayedMonthAsync(DateTime anyDateInMonth)
    {
        DisplayedMonth = new DateTime(anyDateInMonth.Year, anyDateInMonth.Month, 1);

        await LoadAsync();
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
        OnPropertyChanged(nameof(SelectedDayActionText));
    }
}
