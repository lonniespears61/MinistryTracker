// ---------------------------------------------------------------------------------------------------------------------
// UiConverters.cs
//
// PURPOSE
// - Small XAML value converters used by UI styling.
// - Nullable signatures must match IValueConverter in modern .NET MAUI.
// ---------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MinistryTracker.Views.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        // ConverterParameter: "OnColor;OffColor"
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var parts = (parameter?.ToString() ?? "Black;Transparent").Split(';');

            var onColor = parts.Length > 0 ? parts[0] : "Black";
            var offColor = parts.Length > 1 ? parts[1] : "Transparent";

            return value is bool b && b
                ? Color.FromArgb(onColor)
                : Color.FromArgb(offColor);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToThicknessConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var thickness = parameter is string s && int.TryParse(s, out var n)
                ? n
                : 2;

            return value is bool b && b
                ? new Thickness(thickness)
                : new Thickness(0);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class BoolToFontAttributesConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var attr = parameter?.ToString() ?? "Bold";

            return value is bool b && b
                ? Enum.Parse<FontAttributes>(attr)
                : FontAttributes.None;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}