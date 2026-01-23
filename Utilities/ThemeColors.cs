using Microsoft.Maui.Graphics;

namespace MinistryTracker.Utilities;

public static class ThemeColors
{
    // Your chosen base colors (locked).
    public static readonly Color DashboardBase = Color.FromArgb("#0090A8");
    public static readonly Color StudentsBase = Color.FromArgb("#F0AFDD");
    public static readonly Color CalendarBase = Color.FromArgb("#F1C2E3");
    public static readonly Color SettingsBase = Color.FromArgb("#00D062");

    public static ThemePalette GetPalette(ThemePage page)
    {
        var baseColor = page switch
        {
            ThemePage.Dashboard => DashboardBase,
            ThemePage.Students => StudentsBase,
            ThemePage.Calendar => CalendarBase,
            ThemePage.Settings => SettingsBase,
            _ => DashboardBase
        };

        // Tune these once, and every page stays consistent.
        // BG: very light, Card: light, Border: subtle
        var bg = ColorHelpers.Tint(baseColor, 0.92f);
        var card = ColorHelpers.Tint(baseColor, 0.86f);
        var border = ColorHelpers.Tint(baseColor, 0.70f);

        return new ThemePalette(baseColor, bg, card, border);
    }
}
