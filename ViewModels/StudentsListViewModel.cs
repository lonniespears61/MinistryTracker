using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;
using System.Collections.ObjectModel;

namespace MinistryTracker.ViewModels
{
    public partial class StudentsListViewModel : ObservableObject
    {
        private readonly DataService _data;

        [ObservableProperty]
        private ObservableCollection<StudentViewModel> students = new();

        public StudentsListViewModel(DataService data)
        {
            _data = data;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            var items = await _data.GetStudentsAsync();
            Students = new ObservableCollection<StudentViewModel>(
                items.OrderBy(s => s.Name).Select(s => new StudentViewModel(s))
            );
        }
    }
}
