using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

[QueryProperty(nameof(StudentIdQuery), "studentId")]
public partial class StudentVisitHistoryPage : ContentPage
{
    private readonly StudentVisitHistoryViewModel _vm;

    public string? StudentIdQuery { get; set; }

    public StudentVisitHistoryPage(StudentVisitHistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!int.TryParse(StudentIdQuery, out var studentId) ||
            studentId <= 0 ||
            !await _vm.LoadAsync(studentId))
        {
            await DisplayAlert("Visit History", "This student could not be loaded.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void OnVisitSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView collectionView)
            collectionView.SelectedItem = null;

        var visit = e.CurrentSelection
            .OfType<VisitHistoryItemViewModel>()
            .FirstOrDefault();

        if (visit is null)
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(UpdateVisitPage)}?visitId={visit.VisitId}");
    }
}
