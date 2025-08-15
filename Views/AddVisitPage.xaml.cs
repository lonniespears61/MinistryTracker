using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class AddVisitPage : ContentPage
    {
        private readonly AddVisitViewModel _vm;

        public AddVisitPage(AddVisitViewModel vm, Student student)
        {
            InitializeComponent();
            _vm = vm;
            _vm.Load(student);
            BindingContext = _vm;
        }
    }
}
