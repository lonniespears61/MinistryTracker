using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging; // optional if you later message events
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using MinistryTracker.Models.Enums;   // StudentStatus
using MinistryTracker.Data;
using MinistryTracker.Models;

namespace MinistryTracker.ViewModels;

public partial class StudentsListViewModel : ObservableObject
{
    private readonly DataService _data;
    private readonly ILogger<StudentsListViewModel>? _log;

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

    // Handy for showing counts in the UI if desired
    public int VisibleCount => FilteredStudents.Count;

    public StudentsListViewModel(DataService data, ILogger<StudentsListViewModel>? log = null)
    {
        _data = data;
        _log = log;
    }

    public async Task LoadAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Fetch ALL students (active/inactive); we filter locally
            var all = await _data.GetStudentsAsync().ConfigureAwait(false);

            // 🧠 Always update UI collections on the UI thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Students.Clear();
                foreach (var s in all)
                    Students.Add(s);

                ApplyFilter(); // will also run on UI thread
            });
        }
        catch (Exception ex)
        {
            _log?.LogError(ex, "Failed to load students.");
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
        // This method might be invoked from property setters on any context.
        // Keep UI mutations on the UI thread.
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(ApplyFilter);
            return;
        }

        var term = (SearchText ?? string.Empty).Trim();

        IEnumerable<Student> query = Students;

        if (IsActiveOnly)
        {
            // Works for both StudentStatus and StudentStatus?
            query = query.Where(s => s.Status == StudentStatus.Active);
        }

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(s =>
                (!string.IsNullOrEmpty(s.Name) && s.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(s.PreferredLanguage) && s.PreferredLanguage.Contains(term, StringComparison.OrdinalIgnoreCase))
            );
        }

        // Optional: stable sort for nicer UX
        query = query.OrderBy(s => s.Name ?? string.Empty);

        // Replace contents without swapping the collection instance
        FilteredStudents.Clear();
        foreach (var s in query)
            FilteredStudents.Add(s);

        // Let the UI know counts changed
        OnPropertyChanged(nameof(VisibleCount));
    }
}
