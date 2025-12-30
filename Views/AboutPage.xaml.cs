using System;
using Microsoft.Maui.Controls;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage(AboutViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;

            // Remove default back arrow
            NavigationPage.SetHasBackButton(this, false);
        }

        private async void OnHomeClicked(object sender, EventArgs e)
        {
            // Return to Dashboard
            await Navigation.PopToRootAsync(animated: true);
        }
    }
}
