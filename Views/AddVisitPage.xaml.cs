using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class AddVisitPage : ContentPage
    {
        private readonly AddVisitViewModel _vm;

        public AddVisitPage(AddVisitViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;
        }

        // Called by StudentsListPage before navigation to prefill the VM
        public void Load(Student student) => _vm.Load(student);
    }
}
