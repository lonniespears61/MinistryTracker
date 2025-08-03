using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class AddStudentPage : ContentPage
    {
        public AddStudentPage()
        {
            InitializeComponent();

            // Assign the ViewModel to the BindingContext of the page
            BindingContext = new AddStudentViewModel();
        }
    }
}
