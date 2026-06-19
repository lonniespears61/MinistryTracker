// DashboardPage.xaml.cs — Dashboard interactions — 2026-01-22

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.Models.DTOs;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;

    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try { await _vm.LoadAsync(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
        => await OpenSettingsModalAsync();

    private async void OnAboutClicked(object sender, EventArgs e)
    {
        // Temporary UX decision: About content is inside Settings.
        await OpenSettingsModalAsync();
    }

    private async void OnAddNewCallClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(AddStudentPage));

    private async void OnCheckOnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        var suggestion = e.CurrentSelection
            .OfType<CheckOnStudentSuggestion>()
            .FirstOrDefault();

        if (suggestion is null)
            return;

        var today = DateTime.Today.ToString("yyyy-MM-dd");
        await Shell.Current.GoToAsync(
            $"{nameof(AddVisitPage)}?studentId={suggestion.StudentId}&date={today}");
    }

    private async void OnVisitSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        var visit = e.CurrentSelection
            .OfType<VisitWithStudent>()
            .FirstOrDefault();

        if (visit is null)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(UpdateVisitPage)}?visitId={visit.VisitId}");
    }

    private async void OnLongOverdueSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        var suggestion = e.CurrentSelection
            .OfType<CheckOnStudentSuggestion>()
            .FirstOrDefault();

        if (suggestion is null)
            return;

        var today = DateTime.Today.ToString("yyyy-MM-dd");
        await Shell.Current.GoToAsync(
            $"{nameof(AddVisitPage)}?studentId={suggestion.StudentId}&date={today}");
    }

    private async void OnOpenMapClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(StudentsMapPage));

    private async Task OpenSettingsModalAsync()
    {
        try
        {
            var settingsPage = MauiProgram.Services.GetRequiredService<SettingsPage>();
            await Shell.Current.Navigation.PushModalAsync(new NavigationPage(settingsPage));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlert("Settings", "Could not open Settings.", "OK");
        }
    }
}
