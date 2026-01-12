using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    [QueryProperty(nameof(StudentIdQuery), "studentId")]
    public partial class EditStudentPage : ContentPage
    {
        private int _lastLoadedStudentId = -1;

        public EditStudentPage(EditStudentViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        // Shell sets this from the query string (text)
        public string? StudentIdQuery { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is not EditStudentViewModel vm)
                return;

            if (!int.TryParse(StudentIdQuery, out var studentId) || studentId <= 0)
            {
                await Toast.Make("Missing or invalid student id.", ToastDuration.Long).Show();
                await Shell.Current.GoToAsync("..");
                return;
            }

            // ✅ Only skip if we're reappearing for the SAME student
            if (_lastLoadedStudentId == studentId)
                return;

            try
            {
                await vm.LoadAsync(studentId);
                _lastLoadedStudentId = studentId;
            }
            catch (Exception ex)
            {
                await Toast.Make($"Load failed: {ex.Message}", ToastDuration.Long).Show();
                await Shell.Current.GoToAsync("..");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (BindingContext is not EditStudentViewModel vm)
                return;

            try
            {
                var ok = await vm.SaveAsync();
                if (ok)
                {
                    await Toast.Make("Saved.", ToastDuration.Short).Show();
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                await Toast.Make($"Error: {ex.Message}", ToastDuration.Long).Show();
            }
        }
    }
}
