using System;
using System.Linq;
using Microsoft.Maui.Controls;

namespace MinistryTracker.Utilities
{
    /// <summary>
    /// XAML: ItemsSource="{utils:EnumValues {x:Type enums:StudentStatus}}"
    /// Returns an array of the enum's values in declared order.
    /// </summary>
    [ContentProperty(nameof(EnumType))]
    public sealed class EnumValuesExtension : IMarkupExtension<object>
    {
        public Type? EnumType { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (EnumType is null)
                throw new InvalidOperationException("EnumValues requires EnumType, e.g., {utils:EnumValues {x:Type enums:StudentStatus}}.");

            if (!EnumType.IsEnum)
                throw new InvalidOperationException($"{EnumType} is not an enum.");

            // GetValues returns Array of the enum's underlying values in declared order
            return Enum.GetValues(EnumType);
        }
    }
}
