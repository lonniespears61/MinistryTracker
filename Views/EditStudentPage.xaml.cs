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
            BindingContext = _vm = vm;
        }

        // Call this right after resolving the page from DI
        public void Init(Student student) => _vm.Load(student);
    }
}
