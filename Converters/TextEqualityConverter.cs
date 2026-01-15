using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace eVerse.Converters
{
    // Compares two strings and returns a Brush for background: green when equal, transparent otherwise
    public class TextEqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return new SolidColorBrush(Colors.Transparent);

            var a = values[0] as string ?? string.Empty;
            var b = values[1] as string ?? string.Empty;

            return string.Equals(a, b, StringComparison.Ordinal)
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1E, 0x9B, 0xFA))
                : new SolidColorBrush(System.Windows.Media.Colors.Transparent);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
