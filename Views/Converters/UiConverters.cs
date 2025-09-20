using System.Globalization;
namespace MinistryTracker.Views.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        // ConverterParameter: "OnColor;OffColor"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parts = (parameter?.ToString() ?? "Black;Transparent").Split(';');
            return (value is bool b && b) ? Color.FromArgb(parts[0]) : Color.FromArgb(parts[1]);
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class BoolToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var thick = parameter is string s && int.TryParse(s, out var n) ? n : 2;
            return (value is bool b && b) ? new Thickness(thick) : new Thickness(0);
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    public class BoolToFontAttributesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var attr = parameter?.ToString() ?? "Bold";
            return (value is bool b && b) ? Enum.Parse<FontAttributes>(attr) : FontAttributes.None;
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
