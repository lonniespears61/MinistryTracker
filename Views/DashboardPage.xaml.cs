// DashboardPage.xaml.cs — Dashboard interactions — 2026-01-22

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.Models.DTOs;
using MinistryTracker.Services;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _vm;
    private readonly VisitWorkflowCoordinator _workflow;

    public DashboardPage(
        DashboardViewModel vm,
        VisitWorkflowCoordinator workflow)
    {
        InitializeComponent();
        _vm = vm;
        _workflow = workflow;
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

        await _workflow.BeginSchedulingAsync(
            this,
            suggestion.StudentId,
            DateTime.Today);
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

        await _workflow.BeginSchedulingAsync(
            this,
            suggestion.StudentId,
            DateTime.Today);
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
