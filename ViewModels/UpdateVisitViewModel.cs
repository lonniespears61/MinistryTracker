// ---------------------------------------------------------------------------------------------------------------------
// UpdateVisitViewModel.cs
//
// PURPOSE
// - Owns the edit/manage logic for one existing visit.
// - Loads visit details by visitId.
// - Saves same-record edits for allowed fields.
// - Cancels the current visit.
// - Owns reschedule execution using DataService.
//
// DESIGN RULES
// - ViewModel must NOT inherit from ContentPage.
// - Same-record edits are limited to:
//     * method
//     * meeting address
//     * notes
// - Reschedule is NOT an in-place datetime edit.
//   It closes the current record as Rescheduled and creates a new Scheduled record.
// - Cancel from this page means "Canceled by Me" unless UX later adds a choice.
// - Page owns prompts / navigation shell behavior.
// - ViewModel owns business logic + data calls.
//
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class UpdateVisitViewModel : ObservableObject
    {
        private readonly DataService _data;

        public UpdateVisitViewModel(DataService data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>
        /// Raised when same-record edits save successfully.
        /// The page may use this to show confirmation or navigate.
        /// </summary>
        public event Action? SaveCompleted;

        /// <summary>
        /// Raised when the visit is canceled successfully.
        /// The page may use this to navigate away or refresh surrounding context.
        /// </summary>
        public event Action? CancelCompleted;

        /// <summary>
        /// Raised when the user chooses to start reschedule flow.
        /// The page should navigate to Calendar in reschedule mode.
        /// </summary>
        public event Action<int, int>? RescheduleRequested;

        /// <summary>
        /// Raised when the reschedule operation completes successfully.
        /// The page may use this to navigate away or refresh.
        /// </summary>
        public event Action? RescheduleCompleted;

        /// <summary>
        /// Raised when an operation fails.
        /// The page can decide how to show the message.
        /// </summary>
        public event Action<string>? OperationFailed;

        // =====================================================================
        // CONTEXT
        // =====================================================================

        [ObservableProperty]
        private int visitId;

        [ObservableProperty]
        private int studentId;

        [ObservableProperty]
        private string studentName = string.Empty;

        // =====================================================================
        // CURRENT VISIT STATE
        // =====================================================================

        [ObservableProperty]
        private DateTime scheduledDateTime;

        [ObservableProperty]
        private VisitStatus status = VisitStatus.Scheduled;

        // =====================================================================
        // EDITABLE FIELDS
        // =====================================================================

        [ObservableProperty]
        private ContactMethod method = ContactMethod.InPerson;

        [ObservableProperty]
        private string? meetingAddress;

        [ObservableProperty]
        private string? notes;

        // =====================================================================
        // UI STATE
        // =====================================================================

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string? statusMessage;

        // =====================================================================
        // PICKER SOURCES
        // =====================================================================

        public List<ContactMethod> ContactMethodValues =>
            Enum.GetValues<ContactMethod>().ToList();

        // =====================================================================
        // DISPLAY HELPERS
        // =====================================================================

        public string CurrentVisitDateTimeDisplay =>
            ScheduledDateTime == default
                ? "No scheduled time loaded"
                : ScheduledDateTime.ToString("dddd, MMM d • h:mm tt");

        public string CurrentVisitStatusDisplay =>
            Status switch
            {
                VisitStatus.Scheduled => "Scheduled",
                VisitStatus.Successful => "Successful",
                VisitStatus.Missed => "Missed",
                VisitStatus.CanceledByMe => "Canceled (Me)",
                VisitStatus.CanceledByThem => "Canceled (Them)",
                VisitStatus.Rescheduled => "Rescheduled",
                _ => Status.ToString()
            };

        // =====================================================================
        // LOAD
        // =====================================================================

        public async Task LoadAsync(int visitId)
        {
            if (IsBusy)
                return;

            if (visitId <= 0)
            {
                StatusMessage = "Visit id is missing or invalid.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = null;

                var visit = await _data.GetVisitByIdAsync(visitId).ConfigureAwait(false);
                if (visit is null)
                {
                    StatusMessage = "Visit not found.";
                    OperationFailed?.Invoke(StatusMessage);
                    return;
                }

                var student = await _data.GetStudentByIdAsync(visit.StudentId).ConfigureAwait(false);

                VisitId = visit.Id;
                StudentId = visit.StudentId;
                StudentName = student?.Name ?? $"Student #{visit.StudentId}";

                ScheduledDateTime = visit.ScheduledDateTime;
                Status = visit.Status;

                Method = visit.Method;
                MeetingAddress = visit.MeetingAddress;
                Notes = visit.Notes;

                RefreshComputedProperties();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Load failed: {ex.Message}";
                OperationFailed?.Invoke(StatusMessage);
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
        private async Task SaveAsync()
        {
            if (IsBusy)
                return;

            if (VisitId <= 0)
            {
                StatusMessage = "Visit id is missing or invalid.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = null;

                var rows = await _data.UpdateVisitDetailsAsync(
                    VisitId,
                    Method,
                    MeetingAddress,
                    Notes).ConfigureAwait(false);

                if (rows <= 0)
                {
                    StatusMessage = "Visit not found or no changes were saved.";
                    OperationFailed?.Invoke(StatusMessage);
                    return;
                }

                StatusMessage = "Changes saved.";
                SaveCompleted?.Invoke();

                // Reload from source of truth so display state stays aligned.
                await LoadAsync(VisitId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Save failed: {ex.Message}";
                OperationFailed?.Invoke(StatusMessage);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // =====================================================================
        // CANCEL
        // =====================================================================

        [RelayCommand]
        private async Task CancelVisitAsync()
        {
            if (IsBusy)
                return;

            if (VisitId <= 0)
            {
                StatusMessage = "Visit id is missing or invalid.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = null;

                // Locked rule:
                // Cancel from this page is treated as "Canceled by Me".
                var rows = await _data.CancelVisitByMeAsync(VisitId).ConfigureAwait(false);

                if (rows <= 0)
                {
                    StatusMessage = "Visit not found.";
                    OperationFailed?.Invoke(StatusMessage);
                    return;
                }

                Status = VisitStatus.CanceledByMe;
                RefreshComputedProperties();

                StatusMessage = "Visit canceled.";
                CancelCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Cancel failed: {ex.Message}";
                OperationFailed?.Invoke(StatusMessage);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // =====================================================================
        // RESCHEDULE
        // =====================================================================

        [RelayCommand]
        private void Reschedule()
        {
            // -----------------------------------------------------------------
            // LOCKED RULE:
            // Reschedule is not handled by editing ScheduledDateTime in place.
            // The page should navigate to Calendar in reschedule mode using:
            // - current visit id
            // - current student id
            //
            // The actual data operation is performed later by CompleteRescheduleAsync.
            // -----------------------------------------------------------------
            if (VisitId <= 0 || StudentId <= 0)
            {
                StatusMessage = "Visit or student context is missing.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            RescheduleRequested?.Invoke(VisitId, StudentId);
        }

        /// <summary>
        /// Completes the reschedule after Calendar has produced a replacement date.
        ///
        /// WHY:
        /// The page can stay responsible for navigation and user choice, while the
        /// ViewModel stays responsible for the actual business/data operation.
        /// </summary>
        public async Task CompleteRescheduleAsync(DateTime newScheduledDateTime)
        {
            if (IsBusy)
                return;

            if (VisitId <= 0)
            {
                StatusMessage = "Visit id is missing or invalid.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = null;

                // Locked rule:
                // Reschedule closes current record as Rescheduled and creates a new Scheduled record.
                var replacement = await _data.RescheduleVisitAsync(
                    VisitId,
                    newScheduledDateTime).ConfigureAwait(false);

                Status = VisitStatus.Rescheduled;
                RefreshComputedProperties();

                StatusMessage = "Visit rescheduled.";
                RescheduleCompleted?.Invoke();

                // Refresh this ViewModel from source of truth so the page remains coherent
                // if the user stays on it briefly before navigation completes.
                await LoadAsync(VisitId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Reschedule failed: {ex.Message}";
                OperationFailed?.Invoke(StatusMessage);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // =====================================================================
        // REFRESH HELPERS
        // =====================================================================

        /// <summary>
        /// Re-raise computed display properties after state changes.
        /// </summary>
        public void RefreshComputedProperties()
        {
            OnPropertyChanged(nameof(CurrentVisitDateTimeDisplay));
            OnPropertyChanged(nameof(CurrentVisitStatusDisplay));
        }
    }
}