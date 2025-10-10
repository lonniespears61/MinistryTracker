using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using MinistryTracker.Models; // Student + all enums live here

namespace MinistryTracker.ViewModels
{
    /// <summary>ViewModel for the Student Profile page.</summary>
    public partial class StudentProfileViewModel : ObservableObject
    {
        // Core entity
        [ObservableProperty]
        private Student? model;

        public StudentProfileViewModel() { }

        /// <summary>Alias for older XAML that bound to StudentModel.</summary>
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

        public Color StatusColor
        {
            get
            {
                var status = Model != null ? Model.Status.ToString() : string.Empty;

                return status switch
                {
                    "Active" => Colors.Green,
                    "Paused" => Colors.Orange,
                    "NotInterested" => Colors.Gray,
                    _ => Colors.Gray
                };
            }
        }

        public Color InterestColor
        {
            get
            {
                var interest = Model != null ? Model.InterestLevel.ToString() : string.Empty;

                return interest switch
                {
                    "Potential" => Colors.LightGray,
                    "Interested" => Color.FromArgb("#FAFAD2"), // LightGoldenrodYellow
                    "Study" => Colors.LightGreen,
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
