using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

namespace MinistryTracker.Utilities;

public sealed record ThemePalette(
    Color Base,
    Color BgTint,
    Color CardTint,
    Color BorderTint,
    LinearGradientBrush SoftGradient,
    LinearGradientBrush AccentGradient
);