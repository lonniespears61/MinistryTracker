using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;

namespace MinistryTracker.ViewModels
{
    /// <summary>
    /// ViewModel for editing basic student info. Navigation is handled by the page (code-behind),
    /// keeping the VM UI-agnostic (matches your original pattern).
    /// </summary>
    public partial class EditStudentViewModel : ObservableObject
    {
        private readonly DataService _data;

        public EditStudentViewModel(DataService data) => _data = data;

        // --- Bindable fields (keep to original scope for now) ---
        [ObservableProperty] private int studentId;
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private DateTime firstContactDate = DateTime.Today;
        [ObservableProperty] private InitialCallType callType;

        /// <summary>Populate the form from an existing Student.</summary>
        public void Load(Student s)
        {
            StudentId = s.StudentId;
            Name = s.Name;
            FirstContactDate = s.FirstContactDate;
            callType = s.CallType;   // assign backing field to avoid duplicate notifications
            OnPropertyChanged(nameof(CallType));
        }

        /// <summary>Persist changes (no navigation here).</summary>
        [RelayCommand]
        public async Task<bool> SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Name is required.");

            var updated = new Student
            {
                StudentId = StudentId,
                Name = Name.Trim(),
                FirstContactDate = FirstContactDate,
                CallType = CallType
            };

            await _data.UpdateStudentAsync(updated);
            return true;
        }
    }
}
