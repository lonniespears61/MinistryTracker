using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;                  // Color/Colors
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel used to display student data in the UI.
    /// Designed for use with lists, detail views, etc.
    ///
    /// Added for "Tap = Next Visit" UX:
    /// - NextFutureVisitId
    /// - NextFutureVisitDisplay
    ///
    /// Populated by StudentsListViewModel so the UI can decide:
    ///   • If a future visit exists -> open UpdateVisitPage
    ///   • Otherwise -> open AddVisitPage
    /// </summary>
    public class StudentViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private Student _student;

        // ---------------------------------------------------------------------
        // NEW: Next Visit fields (list-level UX state, not stored in DB)
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

        /// <summary>
        /// Constructor accepts a Student model and exposes its data via bindable properties.
        /// </summary>
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

                    // Defensive refresh: not derived from the model, but safe to re-raise.
                    OnPropertyChanged(nameof(NextFutureVisitId));
                    OnPropertyChanged(nameof(NextFutureVisitDisplay));
                }
            }
        }

        /// <summary>
        /// Canonical student identifier (matches Student.StudentId exactly).
        /// </summary>
        public int StudentId => _student.StudentId;

        /// <summary>The student's name.</summary>
        public string Name => _student.Name;

        /// <summary>Optional address for display.</summary>
        public string? StudyAddress => _student.StudyAddress;

        public string FirstContactFormatted => $"Contacted: {_student.FirstContactDate:MMM dd, yyyy}";

        /// <summary>Study location as a string label.</summary>
        public string StudyLocationLabel => _student.StudyLocationType.ToString();

        /// <summary>Border color representing the student's status.</summary>
        public Color StatusBorderColor => _student.Status switch
        {
            StudentStatus.Active => Colors.ForestGreen,
            StudentStatus.Paused => Colors.DarkOrange,
            StudentStatus.NotInterested => Colors.Gray,
            _ => Colors.LightGray
        };

        /// <summary>Background color representing the student's status.</summary>
        public Color StatusBackgroundColor => _student.Status switch
        {
            StudentStatus.Active => Color.FromArgb("#e6ffe6"),         // Light green
            StudentStatus.Paused => Color.FromArgb("#fffbe6"),         // Light orange
            StudentStatus.NotInterested => Color.FromArgb("#f2f2f2"),  // Light gray
            _ => Colors.White
        };

        /// <summary>Color code based on interest level (used for card styling).</summary>
        public Color InterestColor => _student.InterestLevel switch
        {
            InterestLevel.Potential => Colors.LightGray,
            InterestLevel.Interested => Color.FromArgb("#FAFAD2"),     // LightGoldenrodYellow
            InterestLevel.Study => Colors.LightGreen,
            _ => Colors.White
        };

        /// <summary>Initials derived from the student's name (used for avatar badges).</summary>
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_student.Name))
                    return "?";

                var parts = _student.Name
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(p => p.Length > 0)
                    .Select(p => char.ToUpperInvariant(p[0]));

                var initials = new string(parts.Take(2).ToArray());
                return string.IsNullOrWhiteSpace(initials) ? "?" : initials;
            }
        }

        /// <summary>Supplemental text for list rows (language, first contact, etc.).</summary>
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
