using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace MinistryTracker.Converters
{
    /// <summary>
    /// Inverts a boolean value.
    ///
    /// WHY THIS EXISTS:
    /// XAML bindings cannot express logical negation (e.g. "!HasTodayVisits").
    /// This converter allows the UI to react to the FALSE case of a boolean.
    ///
    /// Example:
    /// HasTodayVisits = false → Convert → true → control becomes visible
    ///
    /// Without this, we would need duplicate ViewModel properties or code-behind logic.
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // We expect a boolean from the binding.
            // If it's true → return false
            // If it's false → return true
            if (value is bool boolValue)
                return !boolValue;

            // Defensive fallback:
            // If binding fails or value is not a bool, do not render the control.
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Not used in this app, but implemented for completeness.
            // Keeps behavior symmetric if ever used in two-way binding.
            if (value is bool boolValue)
                return !boolValue;

            return false;
        }
    }
}
