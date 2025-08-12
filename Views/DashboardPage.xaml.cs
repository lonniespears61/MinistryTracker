using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class DashboardPage : ContentPage
    {
        private readonly IServiceProvider _services;

        public DashboardPage(DashboardViewModel vm, IServiceProvider services)
        {
            InitializeComponent();
            BindingContext = vm;
            _services = services;
        }

        private async void OnActiveStudentsTapped(object sender, EventArgs e)
        {
            var page = _services.GetRequiredService<StudentsListPage>();
            await Navigation.PushAsync(page);
        }
    }
}
