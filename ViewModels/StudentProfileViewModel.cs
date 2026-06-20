// ---------------------------------------------------------------------------------------------------------------------
// StudentProfileViewModel.cs
//
// PURPOSE
// - Read-only presentation ViewModel for the Student Profile page.
// - Wraps a Student model and exposes safe UI-friendly computed properties.
//
// DESIGN RULES
// - No legacy fields (DoNotCall / NoLongerInterested)
// - Student profile shows person-level information only
// - Visit history / next visit belong elsewhere
// - Keeps compatibility with older XAML via StudentModel alias
//
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;
using MinistryTracker.Data.Repositories;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Services;

namespace MinistryTracker.ViewModels
{
    public partial class StudentProfileViewModel : ObservableObject
    {
        private readonly IStudentRepository _students;

        [ObservableProperty]
        private Student? model;

        public StudentProfileViewModel(IStudentRepository students)
        {
            _students = students;
        }

        /// <summary>
        /// Compatibility alias for older XAML that bound to StudentModel.
        /// </summary>
        public Student? StudentModel
        {
            get => Model;
            set
            {
                if (value != null)
                    Load(value);
            }
        }

        /// <summary>
        /// Initialize or refresh the profile with a Student.
        /// </summary>
        public void Load(Student student)
        {
            Model = student;
        }

        public async Task<bool> LoadAsync(int studentId)
        {
            var student = await _students.GetStudentByIdAsync(studentId);
            if (student is null)
                return false;

            Load(student);
            return true;
        }

        partial void OnModelChanged(Student? value)
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(PhoneNumber));
            OnPropertyChanged(nameof(HasPhoneNumber));
            OnPropertyChanged(nameof(FirstContactFormatted));
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(CanScheduleVisit));
            OnPropertyChanged(nameof(ScheduleGuidance));
            OnPropertyChanged(nameof(HasScheduleGuidance));
            OnPropertyChanged(nameof(Initials));
            OnPropertyChanged(nameof(SubTitle));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(HasNotes));
        }

        public string Name => Model?.Name ?? string.Empty;

        public string? PhoneNumber => Model?.PhoneNumber;

        public bool HasPhoneNumber => !string.IsNullOrWhiteSpace(Model?.PhoneNumber);

        public string FirstContactFormatted =>
            Model?.FirstContactDate is DateTime d && d != default
                ? $"Contacted: {d:MMM dd, yyyy}"
                : "Contacted: —";

        public string StatusLabel => Model?.Status.ToString() ?? "—";

        public string Notes => Model?.Notes ?? string.Empty;

        public bool HasNotes => !string.IsNullOrWhiteSpace(Model?.Notes);

        public bool CanScheduleVisit =>
            WorkflowPolicy.CanScheduleStudent(Model);

        public string ScheduleGuidance =>
            Model?.Status switch
            {
                StudentStatus.Paused =>
                    "Scheduling is paused for this student. Set the student to Active to schedule a visit.",
                StudentStatus.Completed =>
                    "This student is marked Completed. Reopen as Active to schedule another visit.",
                StudentStatus.Discontinued =>
                    "This student is marked Discontinued. Reopen as Active to schedule another visit.",
                _ => string.Empty
            };

        public bool HasScheduleGuidance =>
            !string.IsNullOrWhiteSpace(ScheduleGuidance);

        public Color StatusColor
        {
            get
            {
                var status = Model?.Status ?? StudentStatus.Active;

                return status switch
                {
                    StudentStatus.Active => Colors.Green,
                    StudentStatus.Paused => Colors.Orange,
                    StudentStatus.Discontinued => Colors.DarkGray,
                    StudentStatus.Completed => Colors.SteelBlue,
                    _ => Colors.Gray
                };
            }
        }

        public string Initials
        {
            get
            {
                var name = Model?.Name;
                if (string.IsNullOrWhiteSpace(name))
                    return "?";

                var parts = name
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
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
                var segments = new List<string>();

                if (!string.IsNullOrWhiteSpace(PhoneNumber))
                    segments.Add($"Phone: {PhoneNumber}");

                if (Model?.FirstContactDate is DateTime d && d != default)
                    segments.Add($"First contact: {d:MMM dd, yyyy}");

                if (Model is not null)
                    segments.Add(Model.InitialContactType.ToString());

                return string.Join(" • ", segments);
            }
        }
    }
}
