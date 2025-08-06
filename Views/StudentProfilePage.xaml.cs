using MinistryTracker.Models;
using MinistryTracker.ViewModels;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace MinistryTracker.Views
{
    public partial class StudentProfilePage : ContentPage
    {
        public StudentProfilePage(Student student)
        {
            InitializeComponent();
            BindingContext = new StudentProfileViewModel(student);
        }

        private async void OnEditStudentClicked(object sender, EventArgs e)
        {
            var toast = Toast.Make("Edit feature is a coming attraction!", ToastDuration.Short, 14);
            await toast.Show();
        }

        private async void OnAddVisitClicked(object sender, EventArgs e)
        {
            var toast = Toast.Make("Add Visit feature coming soon!", ToastDuration.Short, 14);
            await toast.Show();
        }
    }
}
