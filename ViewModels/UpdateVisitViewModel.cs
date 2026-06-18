// ---------------------------------------------------------------------------------------------------------------------
// UpdateVisitViewModel.cs
//
// PURPOSE
// - Owns the edit/manage logic for one existing visit.
// - Loads visit details by visitId.
// - Saves same-record edits for allowed fields.
// - Cancels the current visit.
// - Starts the calendar-based reschedule flow.
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
using MinistryTracker.Data.Repositories;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class UpdateVisitViewModel : ObservableObject
    {
        private readonly IStudentRepository _students;
        private readonly IVisitRepository _visits;

        public UpdateVisitViewModel(IStudentRepository students, IVisitRepository visits)
        {
            _students = students ?? throw new ArgumentNullException(nameof(students));
            _visits = visits ?? throw new ArgumentNullException(nameof(visits));
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

        public event Action<string>? OutcomeCompleted;

        /// <summary>
        /// Raised when the user chooses to start reschedule flow.
        /// The page should navigate to Calendar in reschedule mode.
        /// </summary>
        public event Action<int, int>? RescheduleRequested;

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

        public bool IsPastVisit =>
            ScheduledDateTime != default &&
            ScheduledDateTime <= DateTime.Now;

        public bool CanEditDetails =>
            VisitId > 0 &&
            Status != VisitStatus.Rescheduled;

        public bool CanSetOutcome =>
            IsPastVisit &&
            Status is VisitStatus.Scheduled or VisitStatus.Successful or VisitStatus.Missed;

        public bool CanCancelVisit =>
            Status == VisitStatus.Scheduled &&
            ScheduledDateTime >= DateTime.Now;

        public bool CanRescheduleVisit =>
            Status == VisitStatus.Missed ||
            (Status == VisitStatus.Scheduled && ScheduledDateTime >= DateTime.Now);

        public bool IsHistoryLocked => Status == VisitStatus.Rescheduled;

        public string VisitActionGuidance =>
            IsHistoryLocked
                ? "This is the original record of a rescheduled visit and is read-only."
                : CanSetOutcome
                    ? "Record the outcome or correct the visit details."
                    : Status is VisitStatus.CanceledByMe or VisitStatus.CanceledByThem
                        ? "Canceled visit details and notes may be corrected."
                        : string.Empty;

        public bool HasVisitActionGuidance =>
            !string.IsNullOrWhiteSpace(VisitActionGuidance);

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

                var visit = await _visits.GetVisitByIdAsync(visitId).ConfigureAwait(false);
                if (visit is null)
                {
                    StatusMessage = "Visit not found.";
                    OperationFailed?.Invoke(StatusMessage);
                    return;
                }

                var student = await _students.GetStudentByIdAsync(visit.StudentId).ConfigureAwait(false);

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

            if (!CanEditDetails)
            {
                StatusMessage = "This historical rescheduled record is read-only.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = null;

                var rows = await _visits.UpdateVisitDetailsAsync(
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

        [RelayCommand]
        private async Task MarkSuccessfulAsync()
        {
            await SetOutcomeAsync(VisitStatus.Successful);
        }

        [RelayCommand]
        private async Task MarkMissedAsync()
        {
            await SetOutcomeAsync(VisitStatus.Missed);
        }

        private async Task SetOutcomeAsync(VisitStatus outcome)
        {
            if (IsBusy)
                return;

            if (!CanSetOutcome)
            {
                StatusMessage = "Only a past scheduled, successful, or missed visit can have its outcome changed.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = null;

                var detailRows = await _visits.UpdateVisitDetailsAsync(
                    VisitId,
                    Method,
                    MeetingAddress,
                    Notes).ConfigureAwait(false);

                if (detailRows <= 0)
                {
                    StatusMessage = "Visit not found.";
                    OperationFailed?.Invoke(StatusMessage);
                    return;
                }

                var rows = outcome == VisitStatus.Successful
                    ? await _visits.MarkVisitSuccessfulAsync(
                        VisitId,
                        Notes,
                        ScheduledDateTime).ConfigureAwait(false)
                    : await _visits.MarkVisitMissedAsync(
                        VisitId,
                        Notes).ConfigureAwait(false);

                if (rows <= 0)
                {
                    StatusMessage = "Visit not found.";
                    OperationFailed?.Invoke(StatusMessage);
                    return;
                }

                Status = outcome;
                StatusMessage = outcome == VisitStatus.Successful
                    ? "Visit marked successful."
                    : "Visit marked missed.";

                RefreshComputedProperties();
                OutcomeCompleted?.Invoke(StatusMessage);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Outcome update failed: {ex.Message}";
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

            if (!CanCancelVisit)
            {
                StatusMessage = "Only an upcoming scheduled visit can be canceled.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = null;

                // Locked rule:
                // Cancel from this page is treated as "Canceled by Me".
                var rows = await _visits.CancelVisitByMeAsync(VisitId).ConfigureAwait(false);

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
            // Calendar owns the date choice and completes the reschedule.
            // -----------------------------------------------------------------
            if (VisitId <= 0 || StudentId <= 0)
            {
                StatusMessage = "Visit or student context is missing.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            if (!CanRescheduleVisit)
            {
                StatusMessage = "Only an upcoming scheduled visit or a missed visit can be rescheduled.";
                OperationFailed?.Invoke(StatusMessage);
                return;
            }

            RescheduleRequested?.Invoke(VisitId, StudentId);
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
            OnPropertyChanged(nameof(IsPastVisit));
            OnPropertyChanged(nameof(CanEditDetails));
            OnPropertyChanged(nameof(CanSetOutcome));
            OnPropertyChanged(nameof(CanCancelVisit));
            OnPropertyChanged(nameof(CanRescheduleVisit));
            OnPropertyChanged(nameof(IsHistoryLocked));
            OnPropertyChanged(nameof(VisitActionGuidance));
            OnPropertyChanged(nameof(HasVisitActionGuidance));
        }
    }
}
