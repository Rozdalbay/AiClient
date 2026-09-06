using System.Globalization;
using System.Windows.Data;

namespace AiDesktopClient.Converters;

public sealed class PercentToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
        {
            return Math.Max(0, Math.Min(percent, 100));
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
