using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using System;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class EditStudentViewModel : ObservableObject
    {
        private readonly DataService _dataService;

        public EditStudentViewModel(Student student, DataService dataService)
        {
            _dataService = dataService;

            StudentId = student.StudentId;
            Name = student.Name;
            CallType = student.CallType;
            FirstContactDate = student.FirstContactDate;
        }

        // DO NOT manually declare these properties elsewhere!
        [ObservableProperty] private int studentId;
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private InitialCallType callType;
        [ObservableProperty] private DateTime firstContactDate;

        [RelayCommand]
        private async Task SaveAsync()
        {
            var updatedStudent = new Student
            {
                StudentId = StudentId,
                Name = Name,
                CallType = CallType,
                FirstContactDate = FirstContactDate
            };

            await _dataService.UpdateStudentAsync(updatedStudent);
            await Shell.Current.GoToAsync("..");
        }
    }
}
