using System;
using Microsoft.Maui.ApplicationModel;

namespace MinistryTracker.ViewModels
{
    // Read-only VM: no INotifyPropertyChanged needed (nothing changes at runtime)
    public sealed class AboutViewModel
    {
        public string AppName { get; } = "Ministry Tracker";

        public string VersionDisplay =>
            $"Version {AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";

        public string Copyright =>
            $"© {DateTime.Now:yyyy} Diakonos Tools. All rights reserved.";

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
}
