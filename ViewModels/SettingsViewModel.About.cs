// SettingsViewModel.About.cs — About content for Settings — 2026-01-22

using Microsoft.Maui.ApplicationModel;
using System;

namespace MinistryTracker.ViewModels;

public partial class SettingsViewModel
{
    public string AppName { get; } = "Ministry Tracker";

    public string VersionDisplay =>
        $"Version {AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";

    // If you want less "corporate" tone, drop "All rights reserved."
    public string Copyright =>
        $"© {DateTime.Now:yyyy} Ministry Toolworks.";

    public string LegalDisclaimers { get; } =
@"INDEPENDENT APPLICATION
Ministry Tracker is an independent application and is not affiliated with, endorsed by, or sponsored by any religious organization, including Jehovah’s Witnesses.

PERSONAL USE
This application is intended for personal organizational use only and is not an official record-keeping system.

DATA & PRIVACY
All data entered into this application is stored locally on the user’s device (unless the user explicitly exports/shares it). The developer does not collect, transmit, or store personal data.

NO WARRANTY
This software is provided “as is”, without warranty of any kind, express or implied. Use at your own risk.

LIMITATION OF LIABILITY
In no event shall the developer be liable for any claim, damages, or other liability arising from the use of the software.";

    public string BetaTesterAgreementText { get; } =
@"BETA TESTING
You understand this is a beta version intended to be tested during normal, real-life ministry use. Features may be incomplete, change, or fail.

REAL-WORLD DATA
You may enter real return-visit information when it is appropriate for your personal ministry. Record only what is reasonably necessary. Avoid unnecessary sensitive details, congregation records, confidential health or financial information, or information unrelated to arranging and remembering your own return visits.

LOCAL ENCRYPTED STORAGE
Identifying, contact, location, demographic, and note fields are encrypted on this device. The developer does not receive this data automatically.

NO CLOUD BACKUP
Android backup is disabled. Uninstalling the app, clearing its storage, losing the device, or losing the device encryption key can permanently destroy all app data. Keep any essential information through an appropriate separate method.

FEEDBACK
You may choose to send feedback to the developer from your email app. You can review and edit the message before sending.

DIAGNOSTICS
If you enable diagnostics, the feedback message may include app version, device/platform version, database schema information, and table row counts. It should not include student names, notes, addresses, or visit details.

NO AUTOMATIC ERROR REPORTING
The app does not automatically send crash reports, errors, or personal data. Feedback is sent only when you tap Send Feedback and complete sending from your email app.

PERSONAL DATA
Do not send names, addresses, phone numbers, email addresses, private notes, database files, or identifiable screenshots in feedback. Describe the problem using redacted or invented examples.";

    public string DataSharingAgreementText { get; } =
@"ON-DEMAND SHARING
You may choose to share a selected student, visit, or scheduled follow-up with another user/device when someone else needs to help with a visit.

USER CONTROL
Sharing should happen only when you intentionally tap a Share action and choose what to send. The app should not automatically share student or visit data in the background.

LIMITED PURPOSE
Shared information should be used only for the specific visit or coverage need, such as vacation or temporary help.

PERSONAL RESPONSIBILITY
Before sharing, review the information and make sure it is appropriate to send to the selected person/device.";
}
