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

        private async void OnActiveStudentsTapped(object sender, EventArgs e)
        {
            var page = MauiProgram.Services.GetRequiredService<StudentsListPage>();
            await Navigation.PushAsync(page);
        }
    }
}
