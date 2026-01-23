using Microsoft.Maui.Graphics;

namespace MinistryTracker.Utilities;

public static class ColorHelpers
{
    /// <summary>
    /// Mixes two colors. t=0 => a, t=1 => b.
    /// </summary>
    public static Color Lerp(Color a, Color b, float t)
    {
        t = Clamp01(t);

        return new Color(
            a.Red + (b.Red - a.Red) * t,
            a.Green + (b.Green - a.Green) * t,
            a.Blue + (b.Blue - a.Blue) * t,
            a.Alpha + (b.Alpha - a.Alpha) * t
        );
    }

    /// <summary>
    /// Lightens a color by mixing it with white.
    /// amount=0 => unchanged, amount=1 => white.
    /// </summary>
    public static Color Tint(Color baseColor, float amount)
        => Lerp(baseColor, Colors.White, amount);

    /// <summary>
    /// Darkens a color by mixing it with black.
    /// amount=0 => unchanged, amount=1 => black.
    /// </summary>
    public static Color Shade(Color baseColor, float amount)
        => Lerp(baseColor, Colors.Black, amount);

    private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
}
