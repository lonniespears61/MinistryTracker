using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class EditStudentPage : ContentPage
    {
        public EditStudentPage(EditStudentViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        // Called by StudentsListPage: editPage.Init(student);
        public void Init(Student student)
        {
            if (BindingContext is EditStudentViewModel vm)
            {
                vm.Name = student.Name;
                vm.FirstContactDate = student.FirstContactDate;

                // If both sides expose CallType, uncomment:
                // vm.CallType = student.CallType;

                // If your VM tracks the entity id:
                // vm.StudentId = student.Id;
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
            => await Navigation.PopAsync();
    }
}
