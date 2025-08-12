using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;

namespace MinistryTracker.ViewModels
{
    public partial class EditStudentViewModel : ObservableObject
    {
        private readonly DataService _data;
        private int _id;

        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private DateTime firstContactDate = DateTime.Today;
        // TODO: add other bound fields as needed (Gender, Status pill, etc.)

        public EditStudentViewModel(DataService data) => _data = data;

        public void Load(Student s)
        {
            _id = s.StudentId;
            Name = s.Name;
            FirstContactDate = s.FirstContactDate;
            // map rest of fields here
        }

        [RelayCommand]
        public async Task SaveAsync()
        {
            var s = new Student
            {
                StudentId = _id,
                Name = Name,
                FirstContactDate = FirstContactDate,
                // map rest of fields here
            };

            await _data.UpdateStudentAsync(s);
            await Application.Current.MainPage.Navigation.PopAsync();
        }
    }
}
