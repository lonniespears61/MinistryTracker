using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Graphics;               // For Color / Colors
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel for displaying a student's profile.
    /// Wraps the Student model so it can be bound in the UI.
    /// </summary>
    public partial class StudentProfileViewModel : ObservableObject
    {
        // This generates a public property: StudentModel (with change notifications)
        [ObservableProperty]
        private Student _studentModel = new();

        public StudentProfileViewModel() { }

        /// <summary>
        /// Load a student into the VM.
        /// IMPORTANT: assign to the PROPERTY (StudentModel), not the field,
        /// so bindings are notified.
        /// </summary>
        public void Load(Student student) => StudentModel = student;

        /// <summary>
        /// Computed color for the status "pill".
        /// </summary>
        public Color StatusColor => StudentModel.Status switch
        {
            StudentStatus.Active => Colors.Green,
            StudentStatus.Discontinued => Colors.Red,
            StudentStatus.Paused => Colors.Orange,
            StudentStatus.NotInterested => Colors.Gray,
            _ => Colors.Gray
        };

        /// <summary>
        /// Text to display inside the status pill (keeps XAML simple).
        /// </summary>
        public string StatusText => StudentModel.Status.ToString();

        /// <summary>
        /// Friendly display of the first contact date.
        /// </summary>
        public string FirstContactFormatted => $"Contacted: {StudentModel.FirstContactDate:MMM dd, yyyy}";

        // When StudentModel changes, also notify that StatusColor/StatusText changed.
        partial void OnStudentModelChanged(Student value)
        {
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(FirstContactFormatted));
        }
    }
}
