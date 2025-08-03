using MinistryTracker.Views;
using MinistryTracker.Data;
using System.Diagnostics;
using MinistryTracker.ViewModels;   

namespace MinistryTracker
{
    /// <summary>
    /// Application entry point. Initializes services, data, and root UI.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Static reference to the shared data service instance.
        /// Allows global access throughout the app.
        /// </summary>
        public static DataService Database { get; private set; } = null!;

        /// <summary>
        /// Constructor for the App class. Initializes UI and data services.
        /// </summary>
        public App()
        {
            InitializeComponent();

            // 🧱 Instantiate the SQLite data service (constructor handles file path internally)
            Database = new DataService();

            // 🚀 Begin DB setup in the background to avoid blocking startup
            InitializeDatabaseAsync();
        }

        /// <summary>
        /// Starts asynchronous database initialization.
        /// This runs in the background and doesn't block UI rendering.
        /// </summary>
        private static async void InitializeDatabaseAsync()
        {
            try
            {
                await Database.InitializeAsync();
                Debug.WriteLine("✅ Database initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Database initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up the main window with navigation support.
        /// Called automatically on app launch.
        /// </summary>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            var dashboardViewModel = new DashboardViewModel(App.Database); // ✅ pass dataService
            var dashboardPage = new DashboardPage(dashboardViewModel);      // ✅ pass viewModel
            var navPage = new NavigationPage(dashboardPage);
            return new Window(navPage);
        }


    }
}
