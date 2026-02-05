// StudentProfileViewModel.cs — Student Profile VM — 2026-02-05

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.ViewModels
{
    /// <summary>ViewModel for the Student Profile page.</summary>
    public partial class StudentProfileViewModel : ObservableObject
    {
        [ObservableProperty]
        private Student? model;

        public StudentProfileViewModel() { }

        /// <summary>
        /// Alias for older XAML that bound to StudentModel.
        /// Keep this to avoid breaking older bindings while we refactor pages.
        /// </summary>
        public Student? StudentModel
        {
            get => Model;
            set { if (value != null) Load(value); }
        }

        /// <summary>Initialize/refresh the profile with a Student.</summary>
        public void Load(Student student) => Model = student;

        // Auto-fire computed bindings whenever Model changes
        partial void OnModelChanged(Student? value)
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(PreferredLanguage));
            OnPropertyChanged(nameof(StudyAddress));
            OnPropertyChanged(nameof(FirstContactFormatted));
            OnPropertyChanged(nameof(StudyLocationLabel));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(InterestColor));
            OnPropertyChanged(nameof(Initials));
            OnPropertyChanged(nameof(SubTitle));

            // ✅ Notes support (for display)
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(HasNotes));
        }

        // ---- Display properties used by XAML ----
        public string Name => Model?.Name ?? string.Empty;
        public string? PreferredLanguage => Model?.PreferredLanguage;
        public string? StudyAddress => Model?.StudyAddress;

        public string FirstContactFormatted =>
            Model?.FirstContactDate is DateTime d && d != default
                ? $"Contacted: {d:MMM dd, yyyy}"
                : "Contacted: —";

        public string StudyLocationLabel => Model?.StudyLocationType.ToString() ?? "—";

        // ✅ Display-only notes (may contain STUDENT + LAST/NEXT sections for now)
        public string Notes => Model?.Notes ?? string.Empty;
        public bool HasNotes => !string.IsNullOrWhiteSpace(Model?.Notes);

        public Color StatusColor
        {
            get
            {
                // Switch on the enum (not strings) so renames don’t break UI.
                var status = Model?.Status ?? StudentStatus.Active;

                return status switch
                {
                    StudentStatus.Active => Colors.Green,
                    StudentStatus.DoNotCall => Colors.Red,

                    // If you have these values in your enum, map them.
                    // If not, they’ll never be hit.
                    StudentStatus.Paused => Colors.Orange,
                    StudentStatus.NotInterested => Colors.Gray,

                    _ => Colors.Gray
                };
            }
        }

        public Color InterestColor
        {
            get
            {
                var interest = Model?.InterestLevel ?? default;

                return interest switch
                {
                    InterestLevel.Potential => Colors.LightGray,
                    InterestLevel.Interested => Color.FromArgb("#FAFAD2"), // LightGoldenrodYellow
                    InterestLevel.Study => Colors.LightGreen,
                    _ => Colors.White
                };
            }
        }

        public string Initials
        {
            get
            {
                var name = Model?.Name;
                if (string.IsNullOrWhiteSpace(name)) return "?";

                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                .Where(p => p.Length > 0)
                                .Select(p => char.ToUpperInvariant(p[0]));

                var two = new string(parts.Take(2).ToArray());
                return string.IsNullOrWhiteSpace(two) ? "?" : two;
            }
        }

        public string SubTitle
        {
            get
            {
                var segs = new List<string>();

                if (!string.IsNullOrWhiteSpace(PreferredLanguage))
                    segs.Add($"Language: {PreferredLanguage}");

                if (Model?.FirstContactDate is DateTime d && d != default)
                    segs.Add($"First contact: {d:MMM dd, yyyy}");

                if (segs.Count == 0 && Model is not null)
                    segs.Add(Model.CallType.ToString());

                return string.Join(" • ", segs);
            }
        }
    }
}
