using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MinistryTracker.Models;
using MinistryTracker.ViewModels; // For StudentViewModel
using System.Linq;
using System.Threading.Tasks;

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
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Student student;

        /// <summary>
        /// Constructor accepts a Student model and exposes its data via bindable properties.
        /// </summary>
        public StudentViewModel(Student student)
        {
            this.student = student;
        }

        /// <summary>
        /// The unique student ID (read-only).
        /// </summary>
        public int StudentId => student.StudentId;

        /// <summary>
        /// The student's name.
        /// </summary>
        public string Name => student.Name;

        /// <summary>
        /// Optional address for display.
        /// </summary>
        public string? StudyAddress => student.StudyAddress;

        /// <summary>
        /// Formatted string for initial contact date.
        /// </summary>
        /// <summary>
        /// Friendly display of contact date for the card view.
        /// </summary>
        public string FirstContactFormatted => student.FirstContactDate.ToString("MMM dd, yyyy");

        /// <summary>
        /// Study location as a string label.
        /// </summary>
        public string StudyLocationLabel => student.StudyLocationType.ToString();

       
        /// <summary>
        /// Color code based on interest level (used for card styling).
        /// </summary>
        public string InterestColor => student.InterestLevel switch
        {
            InterestLevel.Potential => "LightGray",
            InterestLevel.Interested => "LightGoldenrodYellow",
            InterestLevel.Study => "LightGreen",
            _ => "White"
        };

        /// <summary>
        /// Expose the raw model in case the caller needs it.
        /// </summary>
        public Student Model => student;
    }
}
