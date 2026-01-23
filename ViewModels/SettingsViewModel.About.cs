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
        $"© {DateTime.Now:yyyy} Diakonos Tools.";

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
}
