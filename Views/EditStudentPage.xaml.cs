// ---------------------------------------------------------------------------------------------------------------------
// EditStudentPage.xaml.cs (DROP-IN - Shell navigation)
//
// WHAT CHANGED / WHY
// 1) ❌ Removed Init(Student student)
//    ✅ Uses Shell query parameter "studentId" instead.
//    WHY: Shell navigation passes IDs; pages/VMs load their own data.
//
// 2) ❌ Removed Navigation.PopAsync()
//    ✅ Uses Shell.Current.GoToAsync("..") to go back.
//    WHY: Under Shell, Navigation.* can bypass Shell and corrupt back behavior.
//
// 3) ✅ Calls vm.LoadAsync(studentId) on appearing (once per navigation)
//    WHY: In Shell, pages can be cached/reused; OnAppearing is the safe refresh point.
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    // This tells Shell: "studentId" in the route should be assigned to StudentIdQuery.
    // Example route: EditStudentPage?studentId=123
    [QueryProperty(nameof(StudentIdQuery), "studentId")]
    public partial class EditStudentPage : ContentPage
    {
        private bool _loadedOnce;

        public EditStudentPage(EditStudentViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

        // Shell sets this from the query string. We keep it as string because Shell passes text.
        // We parse it safely in OnAppearing.
        public string? StudentIdQuery { get; set; }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Prevent duplicate loads if the page re-appears due to minor lifecycle events.
            if (_loadedOnce) return;

            if (BindingContext is not EditStudentViewModel vm)
                return;

            if (!int.TryParse(StudentIdQuery, out var studentId) || studentId <= 0)
            {
                await Toast.Make("Missing or invalid student id.", ToastDuration.Long).Show();
                await Shell.Current.GoToAsync("..");
                return;
            }

            try
            {
                await vm.LoadAsync(studentId);
                _loadedOnce = true;
            }
            catch (Exception ex)
            {
                await Toast.Make($"Load failed: {ex.Message}", ToastDuration.Long).Show();
                await Shell.Current.GoToAsync("..");
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            // Shell back (one level up)
            await Shell.Current.GoToAsync("..");
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (BindingContext is not EditStudentViewModel vm) return;

            try
            {
                var ok = await vm.SaveAsync();
                if (ok)
                {
                    await Toast.Make("Saved.", ToastDuration.Short).Show();

                    // Shell back after save
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
