// ---------------------------------------------------------------------------------------------------------------------
// StudentProfilePage.xaml.cs (DROP-IN - Shell safe)
//
// FIXES:
// 1) ❌ Removed MauiContext service locator:
//      Application.Current?.Handler?.MauiContext?.Services
//    WHY: MauiContext can be null depending on lifecycle timing, especially under Shell.
//
// 2) ❌ Removed Navigation.PushAsync / PopToRootAsync
//    WHY: Shell owns navigation now. Using Navigation.* bypasses Shell and breaks back behavior.
//
// 3) ✅ Use Shell route navigation with query parameters:
//      EditStudentPage?studentId=123
//
// 4) ✅ Optional: remove explicit back button hiding
//    WHY: Shell manages back behavior. Hiding it can make navigation feel broken.
//    (If you truly want no back button, do it via Shell/NavBar settings intentionally.)
// ---------------------------------------------------------------------------------------------------------------------

using MinistryTracker.Models;
using MinistryTracker.Services;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

[QueryProperty(nameof(StudentIdQuery), "studentId")]
public partial class StudentProfilePage : ContentPage
{
    private readonly StudentProfileViewModel _vm;
    private readonly VisitWorkflowCoordinator _workflow;

    public string? StudentIdQuery { get; set; }

    public StudentProfilePage(
        StudentProfileViewModel vm,
        VisitWorkflowCoordinator workflow)
    {
        InitializeComponent();
        _vm = vm;
        _workflow = workflow;
        BindingContext = _vm;

        // Under Shell, you typically do NOT mess with NavigationPage back button.
        // If you want to control back behavior/visibility, do it through Shell/NavBar settings.
        // NavigationPage.SetHasBackButton(this, false);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!int.TryParse(StudentIdQuery, out var studentId) || studentId <= 0)
            return;

        if (await _vm.LoadAsync(studentId))
            return;

        await DisplayAlert("Student not found", "This student could not be loaded.", "OK");
        await Shell.Current.GoToAsync("..");
    }

    // LEGACY HOME CLICKED (REMOVE OR REPURPOSE)
    // In a Shell + TabBar app, "Home" is simply the Dashboard tab.
    // If your XAML still has a Home button wired to this, either remove the button,
    // or switch tabs via an absolute Shell route (example commented below).
    private async void OnHomeClicked(object sender, EventArgs e)
        => await _workflow.GoToDashboardAsync();

    private async void OnEditStudentClicked(object sender, EventArgs e)
    {
        // We navigate using the StudentId, not by passing the Student model
        // and not by resolving pages via MauiContext.
        if (_vm.Model is not Student student)
            return;

        var studentId = student.StudentId;

        // Shell route navigation:
        // - AppShell must have: Routing.RegisterRoute(nameof(EditStudentPage), typeof(EditStudentPage));
        // - EditStudentPage will receive studentId via QueryProperty or load pattern.
        await Shell.Current.GoToAsync($"{nameof(EditStudentPage)}?studentId={studentId}");
    }

    private async void OnVisitHistoryClicked(object sender, EventArgs e)
    {
        if (_vm.Model is not Student student)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(StudentVisitHistoryPage)}?studentId={student.StudentId}");
    }

    private async void OnScheduleVisitClicked(object sender, EventArgs e)
    {
        if (_vm.Model is not Student student)
            return;

        await _workflow.BeginSchedulingAsync(this, student.StudentId);
    }
}
