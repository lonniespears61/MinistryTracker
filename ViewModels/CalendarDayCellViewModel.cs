// ---------------------------------------------------------------------------------------------------------------------
// CalendarDayCellViewModel.cs (DROP-IN)
//
// PURPOSE:
// - Represents one tile in the calendar month grid.
// - Supports placeholder cells, selection, "today" highlight, and "has visits" dot.
//
// NOTE:
// - Color choices are conservative. You can theme later.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using System;

namespace MinistryTracker.ViewModels;

public partial class CalendarDayCellViewModel : ObservableObject
{
    // Placeholder constructor
    private CalendarDayCellViewModel()
    {
        IsPlaceholder = true;
        Date = DateTime.MinValue;
    }

    public CalendarDayCellViewModel(DateTime date)
    {
        Date = date.Date;
        IsPlaceholder = false;
    }

    public static CalendarDayCellViewModel Placeholder() => new();

    public DateTime Date { get; }

    public bool IsPlaceholder { get; }

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private int visitCount;

    public bool HasVisits => !IsPlaceholder && VisitCount > 0;

    public string DayNumberText => IsPlaceholder ? string.Empty : Date.Day.ToString();

    public FontAttributes DayNumberFontAttributes =>
        IsToday ? FontAttributes.Bold : FontAttributes.None;

    public bool IsToday => !IsPlaceholder && Date == DateTime.Today;

    // ---- Styling hooks used by XAML ------------------------------------------------

    public Color BorderColor =>
        IsPlaceholder ? Colors.Transparent :
        IsSelected ? Colors.Black :
        IsToday ? Colors.DarkSlateBlue :
        Colors.LightGray;

    public Color BackgroundColor =>
        IsPlaceholder ? Colors.Transparent :
        IsSelected ? Color.FromArgb("#E6F0FF") : // light blue tint
        IsToday ? Color.FromArgb("#F3F4F6") :    // subtle highlight
        Colors.Transparent;

    public Color DayNumberTextColor =>
        IsPlaceholder ? Colors.Transparent :
        IsSelected ? Colors.Black :
        Colors.Black;

    public Color DotColor =>
        HasVisits ? Colors.ForestGreen : Colors.Transparent;

    // When VisitCount changes, HasVisits binding should also update.
    partial void OnVisitCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasVisits));
        OnPropertyChanged(nameof(DotColor));
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BorderColor));
        OnPropertyChanged(nameof(BackgroundColor));
        OnPropertyChanged(nameof(DayNumberTextColor));
    }
}
