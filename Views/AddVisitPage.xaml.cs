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

        public void Load(Student student) => _vm.Load(student);
    }
}
