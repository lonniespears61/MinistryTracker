using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using System.Diagnostics;
using System.Linq;

namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel for the Dashboard page.
    /// Displays summary stats such as active student count.
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        // Service responsible for accessing local SQLite data
        private readonly DataService _dataService;

        /// <summary>
        /// Total number of active (non-deleted) students.
        /// This is displayed on the Dashboard.
        /// </summary>
        [ObservableProperty]
        private int activeStudentCount;

        /// <summary>
        /// Initializes the DashboardViewModel with injected data service.
        /// </summary>
        /// <param name="dataService">An instance of the data access service.</param>
        public DashboardViewModel(DataService dataService)
        {
            _dataService = dataService;
            LoadDashboardData(); // Fire and forget; it's okay here for initial UI
        }

        /// <summary>
        /// Loads data needed for dashboard UI components.
        /// </summary>
        private async void LoadDashboardData()
        {
            try
            {
                var students = await _dataService.GetStudentsAsync();
                ActiveStudentCount = students.Count(s => !s.IsDeleted);
                Debug.WriteLine($"[DashboardViewModel] Loaded {ActiveStudentCount} active students.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error loading dashboard data: {ex.Message}");
            }
        }
    }
}
