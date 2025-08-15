using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class EditStudentPage : ContentPage
    {
        private readonly EditStudentViewModel _vm;

        public EditStudentPage(EditStudentViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
           
            BindingContext = _vm;
        }
        public void Load(Student student) => _vm.Load(student);
    }
}
