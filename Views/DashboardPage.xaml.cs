using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage(DashboardViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // 👇 ensure data is pulled every time we land here
            if (_vm.LoadAsyncCommand is not null)
                await _vm.LoadAsyncCommand.ExecuteAsync(null);
            else if (_vm.LoadAsync is not null)
                await _vm.LoadAsync(); // whichever you implemented
        }
        private async void OnActiveStudentsTapped(object sender, EventArgs e)
        {
            var page = MauiProgram.Services.GetRequiredService<StudentsListPage>();
            await Navigation.PushAsync(page);
        }
    }
}
