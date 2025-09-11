# MinistryTracker Architecture Reference

## Platform
- .NET MAUI (multi-platform: Android, iOS, Windows, Mac Catalyst)
- .NET 9 target
- SQLite (sqlite-net-pcl) for data storage
- MVVM pattern (CommunityToolkit.Mvvm)

## Folder Structure
- **Data/** → DataService, SQLite partials
- **Models/** → Student, Visit, Household + Enums
- **Services/** → NavigationService, SettingsService, ExportService
- **Utilities/** → Helpers (Formatter, Validators)
- **ViewModels/** → One ViewModel per page (Dashboard, StudentsList, StudentProfile, Add/Edit Student, Add/Edit Visit, Calendar, Settings)
- **Views/** → XAML pages (Dashboard, StudentsList, StudentProfile, Add/Edit Student, Add/Edit Visit, Calendar, Settings, About)
- **Views/Controls/** → shared UI components (BottomNavBar, StatCard)

## Key Principles
- Separation of concerns: Models (data), ViewModels (logic/state), Views (UI)
- Single SQLite connection managed by DataService
- DI via MauiProgram.cs
- Resources (icons, fonts) organized under /Resources
