// FileName: App.xaml.cs — Application bootstrap + theme resources — 2026-01-22

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.Utilities;

namespace MinistryTracker;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();

        _services = services;

        RegisterThemeResources();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new(_services.GetRequiredService<AppShell>());

    private void RegisterThemeResources()
    {
        AddPaletteResources("Dashboard", ThemeColors.GetPalette(ThemePage.Dashboard));
        AddPaletteResources("Students", ThemeColors.GetPalette(ThemePage.Students));
        AddPaletteResources("Calendar", ThemeColors.GetPalette(ThemePage.Calendar));
        AddPaletteResources("Settings", ThemeColors.GetPalette(ThemePage.Settings));
    }

    private void AddPaletteResources(string keyPrefix, ThemePalette p)
    {
        // Colors
        Resources[$"{keyPrefix}.Base"] = p.Base;
        Resources[$"{keyPrefix}.BgTint"] = p.BgTint;
        Resources[$"{keyPrefix}.CardTint"] = p.CardTint;
        Resources[$"{keyPrefix}.BorderTint"] = p.BorderTint;

        // Brushes (convenience)
        Resources[$"{keyPrefix}.BaseBrush"] = new SolidColorBrush(p.Base);
        Resources[$"{keyPrefix}.BgTintBrush"] = new SolidColorBrush(p.BgTint);
        Resources[$"{keyPrefix}.CardTintBrush"] = new SolidColorBrush(p.CardTint);
        Resources[$"{keyPrefix}.BorderTintBrush"] = new SolidColorBrush(p.BorderTint);
    }
}
