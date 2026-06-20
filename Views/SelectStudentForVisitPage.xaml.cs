using System;
using System.Globalization;
using System.Linq;
using MinistryTracker.Services;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

[QueryProperty(nameof(DateQuery), "date")]
public partial class SelectStudentForVisitPage : ContentPage
{
    private readonly SelectStudentForVisitViewModel _vm;
    private readonly VisitWorkflowCoordinator _workflow;
    private bool _loaded;

    public string? DateQuery { get; set; }

    public SelectStudentForVisitPage(
        SelectStudentForVisitViewModel vm,
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

        if (_loaded) return;
        _loaded = true;

        var visitDate = ParseDateQuery();

        if (visitDate < DateTime.Today)
        {
            await DisplayAlert("Choose another day", "Visits can only be scheduled for today or a future date.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        await _vm.LoadAsync(visitDate);
    }

    private DateTime ParseDateQuery()
    {
        if (!string.IsNullOrWhiteSpace(DateQuery) &&
            DateTime.TryParseExact(DateQuery, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.Date;
        }

        return DateTime.Today;
    }

    private async void OnStudentSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        var student = e.CurrentSelection
            .OfType<StudentViewModel>()
            .FirstOrDefault();

        if (student is null)
            return;

        await _workflow.BeginSchedulingAsync(
            this,
            student.StudentId,
            _vm.VisitDate,
            VisitWorkflowCoordinator.CancelToCalendar);
    }
}
