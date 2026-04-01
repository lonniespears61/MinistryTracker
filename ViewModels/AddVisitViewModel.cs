// ---------------------------------------------------------------------------------------------------------------------
// AddVisitViewModel.cs
//
// PURPOSE
// - Handles creation of a new Visit record.
// - Receives studentId and optional date from Shell query params.
// - Captures the minimum visit data needed for the current Visit model.
//
// DESIGN RULES
// - ViewModel owns data/state/save logic
// - View owns navigation and picker UX
// - Visit must be saved with meaningful Stage + Method, not just date/time
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
    public partial class AddVisitViewModel : ObservableObject, IQueryAttributable
    {
        private readonly DataService _data;

        public AddVisitViewModel(DataService data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));

            VisitDate = DateTime.Today;
            VisitTime = DateTime.Now.TimeOfDay;

            // Sensible defaults
            Stage = VisitStage.ReturnVisit;
            Method = ContactMethod.InPerson;
        }

        // =====================================================================
        // EVENTS
        // =====================================================================

        /// <summary>
        /// Fired when the visit saves successfully.
        /// The View responds by navigating back.
        /// </summary>
        public event Action? SaveCompleted;

        /// <summary>
        /// Fired when save fails.
        /// The View can show an alert.
        /// </summary>
        public event Action<string>? SaveFailed;

        // =====================================================================
        // SHELL QUERY INPUTS
        // =====================================================================

        [ObservableProperty]
        private int studentId;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("studentId", out var rawId) && rawId is not null)
            {
                if (rawId is int id)
                    StudentId = id;
                else if (rawId is string s && int.TryParse(s, out var parsed))
                    StudentId = parsed;
            }

            if (query.TryGetValue("date", out var rawDate) && rawDate is string dateStr)
            {
                if (DateTime.TryParse(dateStr, out var parsedDate))
                    VisitDate = parsedDate.Date;
            }
        }

        // =====================================================================
        // FORM FIELDS
        // =====================================================================

        [ObservableProperty]
        private DateTime visitDate;

        [ObservableProperty]
        private TimeSpan visitTime;

        [ObservableProperty]
        private VisitStage stage = VisitStage.ReturnVisit;

        [ObservableProperty]
        private ContactMethod method = ContactMethod.InPerson;

        [ObservableProperty]
        private string? meetingAddress;

        [ObservableProperty]
        private string? notes;

        [ObservableProperty]
        private string studentName = "Add Visit";

        [ObservableProperty]
        private bool isBusy;

        // =====================================================================
        // PICKER SOURCES
        // =====================================================================

        public List<VisitStage> VisitStageValues =>
            Enum.GetValues<VisitStage>().ToList();

        public List<ContactMethod> ContactMethodValues =>
            Enum.GetValues<ContactMethod>().ToList();

        // =====================================================================
        // RESET
        // =====================================================================

        /// <summary>
        /// Reset visit-entry fields.
        /// Does not reset StudentId because that is provided by Shell navigation.
        /// </summary>
        public void Reset()
        {
            VisitDate = DateTime.Today;
            VisitTime = DateTime.Now.TimeOfDay;
            Stage = VisitStage.ReturnVisit;
            Method = ContactMethod.InPerson;
            MeetingAddress = null;
            Notes = null;
            IsBusy = false;
        }

        // =====================================================================
        // SAVE
        // =====================================================================

        [RelayCommand]
        private async Task SaveVisitAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                if (StudentId <= 0)
                    throw new InvalidOperationException("StudentId must be set before saving a visit.");

                var visit = new Visit
                {
                    StudentId = StudentId,
                    Stage = Stage,
                    Method = Method,
                    ScheduledDateTime = VisitDate.Date + VisitTime,
                    MeetingAddress = string.IsNullOrWhiteSpace(MeetingAddress) ? null : MeetingAddress.Trim(),
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                    Status = VisitStatus.Scheduled
                };

                await _data.AddVisitAsync(visit).ConfigureAwait(false);

                SaveCompleted?.Invoke();
            }
            catch (Exception ex)
            {
                SaveFailed?.Invoke(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}