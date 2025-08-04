using CommunityToolkit.Mvvm.ComponentModel;
using MinistryTracker.Models;

namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel for displaying a student's profile.
    /// Wraps the Student model so it can be bound in the UI.
    /// </summary>
    public partial class StudentProfileViewModel : ObservableObject
    {
        // ⚠️ DO NOT call this "Student" to avoid ambiguity with the Student class
        [ObservableProperty]
        private Student _studentModel;

        /// <summary>
        /// Constructor that receives a Student model and sets the property.
        /// </summary>
        public StudentProfileViewModel(Student student)
        {
            // ✅ Must assign to the backing field (_studentModel), not the property StudentModel
            _studentModel = student;
        }
    }
}
