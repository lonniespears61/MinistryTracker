using Microsoft.Maui.Controls;
using MinistryTracker.ViewModels;
using MinistryTracker.Views;

namespace MinistryTracker.Views
{
    public partial class DashboardPage : ContentPage
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        /// <summary>
        /// Handles the click event for the "Students" button at the bottom of the dashboard.
        /// Navigates to the student list page.
        /// </summary>
       
        private async void OnActiveStudentsTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new StudentsListPage());
        }

    }
}
