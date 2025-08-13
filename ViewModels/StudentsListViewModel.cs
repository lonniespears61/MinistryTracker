// StudentsListViewModel.cs
// Show Students directly to simplify bindings. FilteredStudents is what the UI binds to.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;                    // <-- Student
using System.Collections.ObjectModel;
using System.Linq;

namespace MinistryTracker.ViewModels
{
    public partial class StudentsListViewModel : ObservableObject
    {
        private readonly DataService _data;

        // Full set loaded from DB
        public ObservableCollection<Student> Students { get; } = new();

        // What the UI shows (after search filter)
        public ObservableCollection<Student> FilteredStudents { get; } = new();

        // Two-way bound to SearchBar.Text
        [ObservableProperty] private string? searchText;

        public StudentsListViewModel(DataService data) => _data = data;

        [RelayCommand]
        public async Task LoadAsync()
        {
            Students.Clear();

            var list = await _data.GetStudentsAsync();
            foreach (var s in list.OrderBy(s => s.Name))
                Students.Add(s);

            ApplyFilter();
        }

        // Auto-called by MVVM Toolkit when SearchText changes
        partial void OnSearchTextChanged(string? value) => ApplyFilter();

        private void ApplyFilter()
        {
            var q = (searchText ?? string.Empty).Trim();
            var query = string.IsNullOrWhiteSpace(q)
                ? Students
                : Students.Where(s => (s.Name ?? string.Empty)
                        .IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

            FilteredStudents.Clear();
            foreach (var s in query)
                FilteredStudents.Add(s);
        }
    }
}
