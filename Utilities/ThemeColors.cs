// ThemeColors.cs — Central theme palette builder — 2026-01-24

using Microsoft.Maui.Controls; // LinearGradientBrush, GradientStop, GradientStopCollection
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
        var bg = ColorHelpers.Tint(baseColor, 0.95f);
        var card = ColorHelpers.Tint(baseColor, 0.85f);
        var border = ColorHelpers.Tint(baseColor, 0.70f);

        // Gentle depth for large surfaces
        var softGradient = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(bg,   0.0f),
                new GradientStop(card, 1.0f)
            },
            new Point(0, 0),
            new Point(0, 1)
        );

        // Stronger emphasis (headers / primary areas)
        var accentGradient = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(ColorHelpers.Tint(baseColor, 0.90f), 0.0f),
                new GradientStop(baseColor,                            1.0f)
            },
            new Point(0, 0),
            new Point(1, 0)
        );

        return new ThemePalette(
            baseColor,
            bg,
            card,
            border,
            softGradient,
            accentGradient
        );
    }
}
