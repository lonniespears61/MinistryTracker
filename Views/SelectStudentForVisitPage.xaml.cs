using System;
using System.Globalization;
using System.Linq;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

[QueryProperty(nameof(DateQuery), "date")]
public partial class SelectStudentForVisitPage : ContentPage
{
    private readonly SelectStudentForVisitViewModel _vm;
    private bool _loaded;

    public string? DateQuery { get; set; }

    public SelectStudentForVisitPage(SelectStudentForVisitViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
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

        var dateString = _vm.VisitDate.ToString("yyyy-MM-dd");
        await Shell.Current.GoToAsync(
            $"{nameof(AddVisitPage)}?studentId={student.StudentId}&date={dateString}");
    }
}
