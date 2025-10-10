using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Graphics;                  // <-- for Color/Colors
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel used to display student data in the UI.
    /// Designed for use with lists, detail views, etc.
    /// </summary>
    public class StudentViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private Student student;

        /// <summary>
        /// Constructor accepts a Student model and exposes its data via bindable properties.
        /// </summary>
        public StudentViewModel(Student student)
        {
            this.student = student;
        }

        /// <summary>Expose the raw model (get/set triggers change notifications).</summary>
        public Student Model
        {
            get => student;
            set
            {
                if (student != value)
                {
                    student = value;
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
                }
            }
        }

        /// <summary>The unique student ID (read-only).</summary>
        public int StudentId => student.StudentId;

        /// <summary>The student's name.</summary>
        public string Name => student.Name;

        /// <summary>Optional address for display.</summary>
        public string? StudyAddress => student.StudyAddress;

        public string FirstContactFormatted => $"Contacted: {student.FirstContactDate:MMM dd, yyyy}";

        /// <summary>Study location as a string label.</summary>
        public string StudyLocationLabel => student.StudyLocationType.ToString();

        /// <summary>Border color representing the student's status.</summary>
        public Color StatusBorderColor => student.Status switch
        {
            StudentStatus.Active => Colors.ForestGreen,
            StudentStatus.Paused => Colors.DarkOrange,
            StudentStatus.NotInterested => Colors.Gray,
            _ => Colors.LightGray
        };

        /// <summary>Background color representing the student's status.</summary>
        public Color StatusBackgroundColor => student.Status switch
        {
            StudentStatus.Active => Color.FromArgb("#e6ffe6"),         // Light green
            StudentStatus.Paused => Color.FromArgb("#fffbe6"),         // Light orange
            StudentStatus.NotInterested => Color.FromArgb("#f2f2f2"),  // Light gray
            _ => Colors.White
        };

        /// <summary>Color code based on interest level (used for card styling).</summary>
        public Color InterestColor => student.InterestLevel switch
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
                if (string.IsNullOrWhiteSpace(student.Name))
                    return "?";

                var parts = student.Name
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

                if (!string.IsNullOrWhiteSpace(student.PreferredLanguage))
                    segments.Add($"Language: {student.PreferredLanguage}");

                if (student.FirstContactDate != default)
                    segments.Add($"First contact: {student.FirstContactDate:MMM dd, yyyy}");

                if (segments.Count == 0)
                    segments.Add(student.CallType.ToString());

                return string.Join(" • ", segments);
            }
        }
    }
}
