using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    /// <summary>
    /// Add Student page.
    /// BindingContext is injected via DI; the ViewModel is not created in XAML.
    /// </summary>
    public partial class AddStudentPage : ContentPage
    {
        public AddStudentPage(AddStudentViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is AddStudentViewModel vm)
            {
                vm.Reset();
            }
        }
    }
}