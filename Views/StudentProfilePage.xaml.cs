using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MinistryTracker.ViewModels;


namespace MinistryTracker.Views
{
    public partial class StudentProfilePage : ContentPage
    {
        public StudentProfilePage(StudentProfileViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
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
