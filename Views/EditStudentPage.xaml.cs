using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class EditStudentPage : ContentPage
    {
        private readonly EditStudentViewModel _vm;

        public EditStudentPage(EditStudentViewModel vm, Student student)
        {
            InitializeComponent();
            _vm = vm;
            _vm.Load(student);   // your VM should expose Load(Student)
            BindingContext = _vm;
        }
    }
}
