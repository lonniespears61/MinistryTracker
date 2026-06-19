// ---------------------------------------------------------------------------------------------------------------------
// AddVisitViewModel.cs
//
// PURPOSE
// - Handles creation of a new Visit record.
// - Receives studentId and optional date from Shell query params.
// - Applies Normal Service Days settings to default visit date/time.
//
// DESIGN RULES
// - ViewModel owns data/state/save logic
// - View owns navigation and picker UX
// - Visit tracks one planned follow-up attempt
// - Visit no longer uses Stage/Type concepts
// - Default visit date/time should follow the user's configured Normal Service Days
//
// CHANGE NOTES
// - Schedule conflicts are enforced centrally by the visit repository.
//
// ---------------------------------------------------------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinistryTracker.Data;
using MinistryTracker.Data.Repositories;
using MinistryTracker.Models;
using MinistryTracker.Models.Enums;
using MinistryTracker.Services;
using MinistryTracker.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MinistryTracker.ViewModels
{
    public partial class AddVisitViewModel : ObservableObject, IQueryAttributable
    {
        private readonly IVisitRepository _visits;
        private readonly SettingsService _settingsService;

        public AddVisitViewModel(IVisitRepository visits, SettingsService settingsService)
        {
            _visits = visits ?? throw new ArgumentNullException(nameof(visits));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            ApplyDefaultVisitDateTime();

            // Sensible default
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
        /// The View responds by showing an alert.
        /// </summary>
        public event Action<string>? SaveFailed;

        // =====================================================================
        // SHELL QUERY INPUTS
        // =====================================================================

        [ObservableProperty]
        private int studentId;

        [ObservableProperty]
        private int? replaceVisitId;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("studentId", out var rawId) && rawId is not null)
            {
                if (rawId is int id)
                    StudentId = id;
                else if (rawId is string s && int.TryParse(s, out var parsed))
                    StudentId = parsed;
            }

            if (query.TryGetValue("replaceVisitId", out var rawReplaceId) && rawReplaceId is not null)
            {
                if (rawReplaceId is int replaceId)
                    ReplaceVisitId = replaceId;
                else if (rawReplaceId is string replaceText &&
                         int.TryParse(replaceText, out var parsedReplaceId))
                    ReplaceVisitId = parsedReplaceId;
            }

            // If a date is explicitly passed in, keep that date
            // but apply that day's configured default time.
            if (query.TryGetValue("date", out var rawDate) && rawDate is string dateStr)
            {
                if (DateTime.TryParse(dateStr, out var parsedDate))
                {
                    var settings = _settingsService.GetServiceDaySettings();
                    var defaultDateTime = VisitSchedulingHelper.GetDefaultVisitDateTime(settings, parsedDate.Date);

                    VisitDate = defaultDateTime.Date;
                    VisitTime = defaultDateTime.TimeOfDay;
                }
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

        public List<ContactMethod> ContactMethodValues =>
            Enum.GetValues<ContactMethod>().ToList();

        // =====================================================================
        // RESET
        // =====================================================================

        /// <summary>
        /// Reset visit-entry fields.
        /// Does not reset StudentId because that is provided by Shell navigation.
        /// Applies the current Normal Service Days settings.
        /// </summary>
        public void Reset()
        {
            ApplyDefaultVisitDateTime();

            Method = ContactMethod.InPerson;
            MeetingAddress = null;
            Notes = null;
            IsBusy = false;
        }

        // =====================================================================
        // DEFAULT DATE/TIME HELPERS
        // =====================================================================

        /// <summary>
        /// Applies the user's configured default Normal Service Day / period.
        /// </summary>
        private void ApplyDefaultVisitDateTime()
        {
            var settings = _settingsService.GetServiceDaySettings();
            var defaultDateTime = VisitSchedulingHelper.GetDefaultVisitDateTime(settings);

            VisitDate = defaultDateTime.Date;
            VisitTime = defaultDateTime.TimeOfDay;
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

                var scheduledDateTime = VisitDate.Date + VisitTime;

                var visit = new Visit
                {
                    StudentId = StudentId,
                    Method = Method,
                    ScheduledDateTime = scheduledDateTime,
                    MeetingAddress = string.IsNullOrWhiteSpace(MeetingAddress) ? null : MeetingAddress.Trim(),
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                    NotesCreatedDateTime = string.IsNullOrWhiteSpace(Notes) ? null : DateTime.Now,
                    Status = VisitStatus.Scheduled
                };

                if (ReplaceVisitId is int replaceVisitId && replaceVisitId > 0)
                {
                    await _visits
                        .ReplaceScheduledVisitAsync(replaceVisitId, visit)
                        .ConfigureAwait(false);
                }
                else
                {
                    await _visits.AddVisitAsync(visit).ConfigureAwait(false);
                }

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
