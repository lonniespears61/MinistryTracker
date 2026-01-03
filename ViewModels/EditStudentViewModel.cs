// ---------------------------------------------------------------------------------------------------------------------
// EditStudentViewModel.cs (DROP-IN)
//
// WHAT CHANGED / WHY
// 1) ❌ Removed Load(Student s)
//    ✅ Replaced with LoadAsync(int studentId)
//    WHY: Shell navigation passes IDs (route params), not full model objects.
//
// 2) ✅ Save updates the existing Student record rather than creating a partial Student.
//    WHY: Creating a new Student with only 4 fields risks overwriting optional fields with null/defaults,
//         depending on how UpdateStudentAsync is implemented.
//
// 3) ✅ Still no navigation here (page handles it)
// ---------------------------------------------------------------------------------------------------------------------

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
        private readonly DataService _data;

        // We keep a private reference so we can preserve fields you are not editing on this screen.
        private Student? _loadedStudent;

        public EditStudentViewModel(DataService data) => _data = data;

        // --- Bindable fields (same UI surface) ---
        [ObservableProperty] private int studentId;
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private DateTime firstContactDate = DateTime.Today;
        [ObservableProperty] private InitialCallType callType;

        [ObservableProperty] private bool isBusy;

        /// <summary>
        /// Loads the student from the DB by ID and populates the edit fields.
        /// Shell-friendly: pages can pass studentId via route parameters.
        /// </summary>
        public async Task LoadAsync(int id)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                // You need a DataService method that retrieves a student by ID.
                // If you don't have it yet, we'll add it next.
                var s = await _data.GetStudentByIdAsync(id).ConfigureAwait(false);

                _loadedStudent = s;

                // Populate bindable properties
                StudentId = s.StudentId;
                Name = s.Name ?? string.Empty;
                FirstContactDate = s.FirstContactDate;
                CallType = s.CallType;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Persist changes. No navigation here.
        /// Returns true if save succeeded.
        /// </summary>
        [RelayCommand]
        public async Task<bool> SaveAsync()
        {
            if (IsBusy) return false;

            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Name is required.");

            try
            {
                IsBusy = true;

                // If we haven't loaded, we can't safely preserve optional fields.
                // In practice, Edit page should always call LoadAsync(studentId) on appearing.
                if (_loadedStudent is null || _loadedStudent.StudentId != StudentId)
                {
                    _loadedStudent = await _data.GetStudentByIdAsync(StudentId).ConfigureAwait(false);
                }

                // Update only the fields this page owns, preserve everything else.
                _loadedStudent.Name = Name.Trim();
                _loadedStudent.FirstContactDate = FirstContactDate;
                _loadedStudent.CallType = CallType;

                await _data.UpdateStudentAsync(_loadedStudent).ConfigureAwait(false);

                return true;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
