using MinistryTracker.Models; // To accept the Student model
using MinistryTracker.ViewModels; // For StudentProfileViewModel
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Diagnostics;
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
            await Toast.Make("Edit Student feature coming soon!").Show();
            Debug.WriteLine("Not OnAddVisitClicked");
        }

        private async void OnAddVisitClicked(object sender, EventArgs e)
        {
            //  await Shell.Current.DisplayToastAsync("Add Visit feature coming soon!", 3000);
            await Toast.Make("Schedule Visit feature coming soon!").Show();
            Debug.WriteLine("OnAddVisitClicked");
        }


    }
}
