using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    /// <summary>
    /// Add Student page. BindingContext is injected via DI; the VM is not created in XAML.
    /// </summary>
    public partial class AddStudentPage : ContentPage
    {
        public AddStudentPage(AddStudentViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
