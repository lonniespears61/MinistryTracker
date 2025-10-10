using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class EditStudentPage : ContentPage
    {
        public EditStudentPage(EditStudentViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        /// <summary>Prefill the form before navigating to this page.</summary>
        public void Init(Student student)
        {
            if (BindingContext is EditStudentViewModel vm)
                vm.Load(student);
        }

        private async void OnCancelClicked(object sender, EventArgs e)
            => await Navigation.PopAsync();

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (BindingContext is not EditStudentViewModel vm) return;

            try
            {
                var ok = await vm.SaveAsync();
                if (ok)
                {
                    await Toast.Make("Saved.", ToastDuration.Short).Show();
                    await Navigation.PopAsync();
                }
            }
            catch (Exception ex)
            {
                await Toast.Make($"Error: {ex.Message}", ToastDuration.Long).Show();
            }
        }
    }
}
