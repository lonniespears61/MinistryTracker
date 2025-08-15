using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using MinistryTracker.Models.Enums; // <-- where StudentStatus lives
using System.Threading.Tasks;
using MinistryTracker.Data;
using MinistryTracker.Models;

public partial class StudentsListViewModel : ObservableObject
{
    private readonly DataService _data;

    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty]
    private ObservableCollection<Student> filteredStudents = new();

    [ObservableProperty]
    private string? searchText;

    // ✅ default ON to mirror Dashboard behavior
    [ObservableProperty]
    private bool isActiveOnly = true;

    [ObservableProperty]
    private bool isBusy;

    public StudentsListViewModel(DataService data)
    {
        _data = data;
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;
        try
        {
            IsBusy = true;

            var all = await _data.GetStudentsAsync(); // should return ALL students
            Students.Clear();
            foreach (var s in all)
                Students.Add(s);

            ApplyFilter();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // 🔄 Pull-to-refresh
    [RelayCommand]
    private async Task Refresh()
    {
        await LoadAsync();
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnIsActiveOnlyChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        var term = (SearchText ?? string.Empty).Trim();
        var query = Students.AsEnumerable();

        if (IsActiveOnly)
        {
            // Adjust predicate to your actual model
            // If you have an enum: s.Status == StudentStatus.Active
            query = query.Where(s => s.Status is StudentStatus.Active);
        }

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(s =>
                (!string.IsNullOrEmpty(s.Name) && s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(s.PreferredLanguage) && s.PreferredLanguage.Contains(term, StringComparison.OrdinalIgnoreCase))
            );
        }

        // Update the existing collection so the binding sees changes
        FilteredStudents.Clear();
        foreach (var s in query)
            FilteredStudents.Add(s);
    }
}
