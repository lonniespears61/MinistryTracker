// StudentViewModel.cs — Student row + detail presentation VM — 2026-01-24

using Microsoft.Maui.Graphics;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel used to display student data in the UI.
    /// Designed for use with lists and detail views.
    ///
    /// List-only UI state:
    /// - IsAlternate (row alternation for list readability)
    ///
    /// Added for "Tap = Next Visit" UX:
    /// - NextFutureVisitId
    /// - NextFutureVisitDisplay
    /// </summary>
    public class StudentViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private Student _student;

        // ---------------------------------------------------------------------
        // LIST-LEVEL UI STATE (not stored in DB)
        // ---------------------------------------------------------------------

        private bool _isAlternate;

        /// <summary>
        /// True if this row should render using the alternate background.
        /// Set by StudentsListViewModel when building the list.
        /// </summary>
        public bool IsAlternate
        {
            get => _isAlternate;
            set
            {
                if (_isAlternate != value)
                {
                    _isAlternate = value;
                    OnPropertyChanged();
                }
            }
        }

        // ---------------------------------------------------------------------
        // NEXT VISIT UX STATE (computed at list level)
        // ---------------------------------------------------------------------

        private int? _nextFutureVisitId;

        /// <summary>
        /// Visit.Id of the student's next FUTURE scheduled visit (null if none).
        /// </summary>
        public int? NextFutureVisitId
        {
            get => _nextFutureVisitId;
            set
            {
                if (_nextFutureVisitId != value)
                {
                    _nextFutureVisitId = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _nextFutureVisitDisplay = "No visit scheduled";

        /// <summary>
        /// Human-friendly label ("Next: Jan 12, 6:30 PM") or "No visit scheduled".
        /// </summary>
        public string NextFutureVisitDisplay
        {
            get => _nextFutureVisitDisplay;
            set
            {
                if (_nextFutureVisitDisplay != value)
                {
                    _nextFutureVisitDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        // ---------------------------------------------------------------------
        // CONSTRUCTION / MODEL WRAPPING
        // ---------------------------------------------------------------------

        public StudentViewModel(Student student)
        {
            _student = student;
        }

        /// <summary>
        /// Expose the raw model (get/set triggers change notifications).
        /// </summary>
        public Student Model
        {
            get => _student;
            set
            {
                if (!ReferenceEquals(_student, value))
                {
                    _student = value;

                    // Notify the UI that the model and computed properties changed.
                    OnPropertyChanged();
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

                    // List-only derived state
                    OnPropertyChanged(nameof(NextFutureVisitId));
                    OnPropertyChanged(nameof(NextFutureVisitDisplay));
                }
            }
        }

        // ---------------------------------------------------------------------
        // SIMPLE MODEL PROJECTIONS
        // ---------------------------------------------------------------------

        public int StudentId => _student.StudentId;

        public string Name => _student.Name;

        public string? StudyAddress => _student.StudyAddress;

        public string FirstContactFormatted
            => $"Contacted: {_student.FirstContactDate:MMM dd, yyyy}";

        public string StudyLocationLabel
            => _student.StudyLocationType.ToString();

        // ---------------------------------------------------------------------
        // STATUS / INTEREST VISUALS
        // ---------------------------------------------------------------------

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

        // ---------------------------------------------------------------------
        // DISPLAY HELPERS
        // ---------------------------------------------------------------------

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
}
