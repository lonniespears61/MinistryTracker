using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class AddVisitPage : ContentPage
    {
        public AddVisitPage(AddVisitViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
