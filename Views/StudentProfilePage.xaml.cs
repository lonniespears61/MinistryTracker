using MinistryTracker.Models; // To accept the Student model
using MinistryTracker.ViewModels; // For StudentProfileViewModel
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
namespace MinistryTracker.Views
{
    /// <summary>
    /// Code-behind for the StudentProfilePage.xaml.
    /// Accepts a Student object and wraps it in a ViewModel.
    /// </summary>
    public partial class StudentProfilePage : ContentPage
    {
        /// <summary>
        /// Constructor that takes a Student model and creates a ViewModel for it.
        /// </summary>
        /// <param name="student">The Student object to show on the profile.</param>
        public StudentProfilePage(Student student)
        {
            InitializeComponent();

            // ✅ Wrap the raw Student in the appropriate ViewModel
            BindingContext = new StudentProfileViewModel(student);
        }
        private async void OnEditStudentClicked(object sender, EventArgs e)
        {
          //  await Shell.Current.DisplayToastAsync("Edit feature is a coming attraction!", 3000);
            var toast = Toast.Make("Edit feature is a coming attraction!", ToastDuration.Short, 14);
            await toast.Show();
        }

        private async void OnAddVisitClicked(object sender, EventArgs e)
        {
          //  await Shell.Current.DisplayToastAsync("Add Visit feature coming soon!", 3000);
            var toast = Toast.Make("Add Visit feature coming soon!", ToastDuration.Short, 14);
            await toast.Show();
        }


    }
}
