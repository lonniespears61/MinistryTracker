// StudentProfileViewModel.cs — Student Profile VM — 2026-03-28

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

        partial void OnModelChanged(Student? value)
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(PhoneNumber));
            OnPropertyChanged(nameof(FirstContactFormatted));
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(InterestLabel));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(InterestColor));
            OnPropertyChanged(nameof(Initials));
            OnPropertyChanged(nameof(SubTitle));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(HasNotes));
        }

        public string Name => Model?.Name ?? string.Empty;

        public string? PhoneNumber => Model?.PhoneNumber;

        public string FirstContactFormatted =>
            Model?.FirstContactDate is DateTime d && d != default
                ? $"Contacted: {d:MMM dd, yyyy}"
                : "Contacted: —";

        public string StatusLabel => Model?.Status.ToString() ?? "—";

        public string InterestLabel => Model?.InterestLevel.ToString() ?? "—";

        public string Notes => Model?.Notes ?? string.Empty;

        public bool HasNotes => !string.IsNullOrWhiteSpace(Model?.Notes);

        public Color StatusColor
        {
            get
            {
                var status = Model?.Status ?? StudentStatus.Active;

                if (Model?.IsDoNotCall == true)
                    return Colors.Red;

                return status switch
                {
                    StudentStatus.Active => Colors.Green,
                    StudentStatus.Paused => Colors.Orange,
                    StudentStatus.NoLongerInterested => Colors.Gray,
                    StudentStatus.Discontinued => Colors.DarkGray,
                    _ => Colors.Gray
                };
            }
        }

        public Color InterestColor
        {
            get
            {
                var interest = Model?.InterestLevel ?? InterestLevel.Promising;

                return interest switch
                {
                    InterestLevel.Promising => Colors.LightGray,
                    InterestLevel.Interested => Color.FromArgb("#FAFAD2"),
                    InterestLevel.ReturnVisit => Colors.LightBlue,
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

                if (!string.IsNullOrWhiteSpace(PhoneNumber))
                    segs.Add($"Phone: {PhoneNumber}");

                if (Model?.FirstContactDate is DateTime d && d != default)
                    segs.Add($"First contact: {d:MMM dd, yyyy}");

                if (Model is not null)
                    segs.Add(Model.InitialContactType.ToString());

                return string.Join(" • ", segs);
            }
        }
    }
}