using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
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
            // 👇 ensure data is pulled every time we land here
            try
            {
                await _vm.LoadAsync();
            } catch (Exception ex) {
                // Log or handle the exception as needed
                log
                { }
        }
        private async void OnActiveStudentsTapped(object sender, EventArgs e)
        {
            var page = MauiProgram.Services.GetRequiredService<StudentsListPage>();
            await Navigation.PushAsync(page);
        }
    }
}
