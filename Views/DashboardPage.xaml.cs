using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    /// <summary>
    /// Dashboard page displaying summary information for the ministry.
    /// The view model is provided via dependency injection and stored in <see cref="_vm"/>.
    /// </summary>
    public partial class DashboardPage : ContentPage
    {
        private readonly DashboardViewModel _vm; // ✅ add this

        public DashboardPage(DashboardViewModel vm)
        {
            InitializeComponent();
            _vm = vm; // ✅ keep a handle to the VM you bound
            BindingContext = _vm;
        }
      
         protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Safely call ViewModel's load logic
            if (_vm != null)
            {
                await _vm.LoadAsync();
            }
        }
        
        /// <summary>
        /// Navigates to the list of active students.
        /// </summary>
        private async void OnActiveStudentsTapped(object sender, EventArgs e)
        {
            var page = MauiProgram.Services.GetRequiredService<StudentsListPage>();
            await Navigation.PushAsync(page);
        }
    }
}
