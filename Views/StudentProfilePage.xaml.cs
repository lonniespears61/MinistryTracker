using MinistryTracker.Models;
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

        // NEW: explicit initializer so callers can inject the student before showing the page
        public void Init(Student student)
        {
            if (BindingContext is StudentProfileViewModel vm)
            {
                vm.Load(student); // populates StudentModel and fires computed bindings
                Title = string.IsNullOrWhiteSpace(student.Name) ? "Student Profile" : student.Name;
            }
        }

        private async void OnEditStudentClicked(object sender, EventArgs e)
        {
            var toast = CommunityToolkit.Maui.Alerts.Toast.Make("Edit feature is a coming attraction!", CommunityToolkit.Maui.Core.ToastDuration.Short, 14);
            await toast.Show();
        }

        private async void OnAddVisitClicked(object sender, EventArgs e)
        {
            var toast = CommunityToolkit.Maui.Alerts.Toast.Make("Add Visit feature coming soon!", CommunityToolkit.Maui.Core.ToastDuration.Short, 14);
            await toast.Show();
        }
    }
}
