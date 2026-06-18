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
// - Enforces a minimum 30-minute gap between scheduled visits.
// - Uses the existing visit range query instead of introducing new data methods.
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

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("studentId", out var rawId) && rawId is not null)
            {
                if (rawId is int id)
                    StudentId = id;
                else if (rawId is string s && int.TryParse(s, out var parsed))
                    StudentId = parsed;
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

            if (query.TryGetValue("returnTo", out var rawReturnTo) && rawReturnTo is not null)
            {
                ReturnTo = rawReturnTo.ToString();
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

        [ObservableProperty]
        private string? returnTo;

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

                // -----------------------------------------------------------------
                // SCHEDULING RULE: VISITS MUST BE AT LEAST 30 MINUTES APART
                //
                // WHY:
                // - The app is for one publisher's real-world schedule.
                // - The user should not be able to schedule two visits too close together.
                // - This applies across ALL students, not just the same student.
                //
                // HOW:
                // - Look 29 minutes backward and 29 minutes forward from the proposed time.
                // - If any scheduled visit already exists in that window, block the save.
                //
                // NOTE:
                // - Exact 30-minute spacing is allowed.
                // - Only scheduled visits count as conflicts.
                // -----------------------------------------------------------------
                var windowStart = scheduledDateTime.AddMinutes(-29);
                var windowEnd = scheduledDateTime.AddMinutes(29);

                var nearbyVisits = await _visits
                    .GetVisitsWithStudentsInRangeAsync(windowStart, windowEnd, includeCanceled: false)
                    .ConfigureAwait(false);

                var hasConflict = nearbyVisits.Any(v =>
                    v.Status == VisitStatus.Scheduled &&
                    Math.Abs((v.ScheduledDateTime - scheduledDateTime).TotalMinutes) < 30);

                if (hasConflict)
                {
                    throw new InvalidOperationException(
                        "You already have a visit scheduled within 30 minutes of this time.");
                }

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

                await _visits.AddVisitAsync(visit).ConfigureAwait(false);

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
