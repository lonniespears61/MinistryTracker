using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using System.Collections.ObjectModel;

namespace MinistryTracker.ViewModels;

public partial class MyCalendarViewModel : ObservableObject
{
    private readonly DataService _dataService;

    public ObservableCollection<MyCalendarItem> Items { get; } = new();

    [ObservableProperty]
    private bool isBusy;

    public MyCalendarViewModel(DataService dataService)
    {
        _dataService = dataService;
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Items.Clear();

            // ✅ Minimal stub to compile today.
            // TODO (when Visit table exists): replace with _dataService.GetScheduledVisitsAsync()
            await Task.Delay(50);

            // Example placeholder row so you can see the page works
            Items.Add(new MyCalendarItem
            {
                StudentId = 1,
                StudentName = "Example Student",
                When = DateTime.Today.AddDays(3).AddHours(10),
                VisitId = 1001
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenVisitAsync(MyCalendarItem item)
    {
        if (item is null) return;

        // Stub navigation target for later:
        // If VisitId exists -> UpdateVisitPage
        // else -> AddVisitPage
        // For now, do nothing but keep it async-safe.
        await Task.CompletedTask;
    }
}

public class MyCalendarItem
{
    public int StudentId { get; set; }
    public int? VisitId { get; set; }
    public string StudentName { get; set; } = "";
    public DateTime When { get; set; }

    public string WhenDisplay => When.ToString("ddd, MMM d • h:mm tt");
}
