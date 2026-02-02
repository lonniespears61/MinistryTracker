// StudentViewModel.cs — Student row + detail presentation VM — 2026-02-01
//
// Purpose:
// - Wraps Student model for UI binding (lists + details)
// - Keeps UI-only state here (selection, alternation, next visit)
//
// Design rules:
// - Uses CommunityToolkit ObservableObject (no manual INotifyPropertyChanged)
// - Model is stored privately; computed properties read from it
// - Provide a RefreshFromModel() hook for when the model is replaced/updated

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinistryTracker.ViewModels;

public partial class StudentViewModel : ObservableObject
{
    // =====================================================================
    // 1) MODEL (DB-backed)
    // =====================================================================

    private Student _student;

    public StudentViewModel(Student student)
    {
        _student = student ?? throw new ArgumentNullException(nameof(student));
    }

    /// <summary>
    /// Raw model. If you replace it, call RefreshFromModel().
    /// (We intentionally do NOT auto-refresh on set to avoid accidental churn.)
    /// </summary>
    public Student Model => _student;

    /// <summary>
    /// Use this when you re-load / update the student and want the UI to refresh
    /// computed properties (Name, Subtitle, colors, etc.).
    /// </summary>
    public void RefreshFromModel(Student updated)
    {
        _student = updated ?? throw new ArgumentNullException(nameof(updated));

        // Re-fire all computed bindings (cheap, simple, reliable)
        OnPropertyChanged(nameof(StudentId));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(StudyAddress));
        OnPropertyChanged(nameof(FirstContactFormatted));
        OnPropertyChanged(nameof(StudyLocationLabel));
        OnPropertyChanged(nameof(StatusBorderColor));
        OnPropertyChanged(nameof(StatusBackgroundColor));
        OnPropertyChanged(nameof(InterestColor));
        OnPropertyChanged(nameof(Initials));
        OnPropertyChanged(nameof(SubTitle));
    }

    // =====================================================================
    // 2) LIST UI STATE (NOT stored in DB)
    // =====================================================================

    [ObservableProperty]
    private bool isAlternate;

    [ObservableProperty]
    private bool isSelected;

    // =====================================================================
    // 3) NEXT VISIT UI STATE (computed outside and assigned here)
    // =====================================================================

    [ObservableProperty]
    private int? nextFutureVisitId;

    [ObservableProperty]
    private string nextFutureVisitDisplay = "No visit scheduled";

    // =====================================================================
    // 4) SIMPLE MODEL PROJECTIONS (safe bind targets)
    // =====================================================================

    public int StudentId => _student.StudentId;

    public string Name => _student.Name ?? string.Empty;

    public string? StudyAddress => _student.StudyAddress;

    public string FirstContactFormatted =>
        _student.FirstContactDate == default
            ? "Contacted: —"
            : $"Contacted: {_student.FirstContactDate:MMM dd, yyyy}";

    public string StudyLocationLabel => _student.StudyLocationType.ToString();

    // =====================================================================
    // 5) STATUS / INTEREST VISUALS
    // =====================================================================

    public Color StatusBorderColor => _student.Status switch
    {
        StudentStatus.Active => Colors.ForestGreen,
        StudentStatus.Paused => Colors.DarkOrange,
        StudentStatus.NotInterested => Colors.Gray,
        _ => Colors.LightGray
    };

    public Color StatusBackgroundColor => _student.Status switch
    {
        StudentStatus.Active => Color.FromArgb("#e6ffe6"),
        StudentStatus.Paused => Color.FromArgb("#fffbe6"),
        StudentStatus.NotInterested => Color.FromArgb("#f2f2f2"),
        _ => Colors.White
    };

    public Color InterestColor => _student.InterestLevel switch
    {
        InterestLevel.Potential => Colors.LightGray,
        InterestLevel.Interested => Color.FromArgb("#FAFAD2"),
        InterestLevel.Study => Colors.LightGreen,
        _ => Colors.White
    };

    // =====================================================================
    // 6) DISPLAY HELPERS (computed strings)
    // =====================================================================

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_student.Name))
                return "?";

            var parts = _student.Name
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => char.ToUpperInvariant(p[0]));

            var initials = new string(parts.Take(2).ToArray());
            return string.IsNullOrWhiteSpace(initials) ? "?" : initials;
        }
    }

    public string SubTitle
    {
        get
        {
            var segments = new List<string>();

            if (!string.IsNullOrWhiteSpace(_student.PreferredLanguage))
                segments.Add($"Language: {_student.PreferredLanguage}");

            if (_student.FirstContactDate != default)
                segments.Add($"First contact: {_student.FirstContactDate:MMM dd, yyyy}");

            if (segments.Count == 0)
                segments.Add(_student.CallType.ToString());

            return string.Join(" • ", segments);
        }
    }
}
