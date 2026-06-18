// ---------------------------------------------------------------------------------------------------------------------
// EditStudentViewModel.cs
//
// PURPOSE
// - Handles editing an existing Student record.
//
// DESIGN RULES
// - Student = identity + relationship state
// - No visit data here
// - Uses current Student model
// - ViewModel owns state + save logic
//
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class EditStudentViewModel : ObservableObject
    {
        private readonly DataService _data;
        private Student? _loadedStudent;

        public EditStudentViewModel(DataService data) => _data = data;

        // =====================================================================
        // CORE FIELDS
        // =====================================================================

        [ObservableProperty] private int studentId;
        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private DateTime firstContactDate = DateTime.Today;
        [ObservableProperty] private InitialContactType initialContactType;

        [ObservableProperty] private string? phoneNumber;
        [ObservableProperty] private ContactMethod? preferredContactMethod;
        [ObservableProperty] private StudentStatus status;
        [ObservableProperty] private string? notes;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? statusMessage;

        // =====================================================================
        // PICKER SOURCES
        // =====================================================================

        public List<InitialContactType> InitialContactTypeValues =>
            Enum.GetValues<InitialContactType>().ToList();

        public List<ContactMethod> ContactMethodValues =>
            Enum.GetValues<ContactMethod>().ToList();

        public List<StudentStatus> StudentStatusValues =>
            Enum.GetValues<StudentStatus>().ToList();

        // =====================================================================
        // LOAD
        // =====================================================================

        public async Task LoadAsync(int id)
        {
            if (IsBusy) return;

            StudentId = id;

            try
            {
                IsBusy = true;

                var s = await _data.GetStudentByIdAsync(id);
                if (s is null)
                {
                    StatusMessage = "Student not found.";
                    return;
                }

                _loadedStudent = s;

                Name = s.Name ?? string.Empty;
                FirstContactDate = s.FirstContactDate;
                InitialContactType = s.InitialContactType;
                PhoneNumber = s.PhoneNumber;
                PreferredContactMethod = s.PreferredContactMethod;
                Status = s.Status;
                Notes = s.Notes;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Load failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // =====================================================================
        // SAVE
        // =====================================================================

        [RelayCommand]
        public async Task<bool> SaveAsync()
        {
            if (IsBusy) return false;

            if (string.IsNullOrWhiteSpace(Name))
            {
                StatusMessage = "Name is required.";
                return false;
            }

            try
            {
                IsBusy = true;

                if (_loadedStudent is null || _loadedStudent.StudentId != StudentId)
                {
                    _loadedStudent = await _data.GetStudentByIdAsync(StudentId);
                }

                if (_loadedStudent is null)
                {
                    StatusMessage = "Student not found.";
                    return false;
                }

                _loadedStudent.Name = Name.Trim();
                _loadedStudent.FirstContactDate = FirstContactDate;
                _loadedStudent.InitialContactType = InitialContactType;
                _loadedStudent.PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim();
                _loadedStudent.PreferredContactMethod = PreferredContactMethod;
                _loadedStudent.Status = Status;
                _loadedStudent.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();

                await _data.UpdateStudentAsync(_loadedStudent);

                StatusMessage = "Saved ✓";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
