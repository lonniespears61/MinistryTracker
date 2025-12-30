using MinistryTracker.ViewModels;

namespace MinistryTracker.Views;

public partial class MyCalendarPage : ContentPage
{
    private readonly MyCalendarViewModel _vm;

    public MyCalendarPage(MyCalendarViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // No Init() on pages. This is fine: page lifecycle calls VM load.
        await _vm.LoadAsync();
    }
}
