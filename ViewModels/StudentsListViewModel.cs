using CommunityToolkit.Mvvm.ComponentModel;
using MinistryTracker.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MinistryTracker.Data; 


namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel responsible for displaying and filtering the list of students.
    /// Uses CommunityToolkit.Mvvm to simplify property notifications.
    /// </summary>
    public partial class StudentsListViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        /// <summary>
        /// The list of students to display in the UI.
        /// Automatically notifies the view when changed.
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<StudentViewModel> students = new();

        /// <summary>
        /// Constructor initializes data service and loads students.
        /// </summary>
        public StudentsListViewModel()
        {
            _dataService = App.Database;
            _ = LoadStudentsAsync();
        }

        /// <summary>
        /// Loads students from the database, wraps them in view models,
        /// and sorts them alphabetically.
        /// </summary>
        private async Task LoadStudentsAsync()
        {
            var studentList = await _dataService.GetStudentsAsync();
            var sorted = studentList.OrderBy(s => s.Name)
                                    .Select(s => new StudentViewModel(s));

            Students = new ObservableCollection<StudentViewModel>(sorted);
        }
    }
}
