using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

public partial class StudentProfilePage : ContentPage
{
    private readonly StudentProfileViewModel _vm;

    public StudentProfilePage(StudentProfileViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;

        NavigationPage.SetHasBackButton(this, false);
    }

    private async void OnHomeClicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync(animated: true);
    }

    private async void OnEditStudentClicked(object sender, EventArgs e)
    {
        if (_vm.Model is not Student student)
            return;

        var sp = Application.Current?.Handler?.MauiContext?.Services;
        var editPage = sp?.GetRequiredService<EditStudentPage>();
        if (editPage is null) return;

        if (editPage.BindingContext is EditStudentViewModel evm)
            evm.Load(student);

        await Navigation.PushAsync(editPage);
    }
}
