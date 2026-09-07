using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AiDesktopClient.Converters;

// строка статуса "Success/Failed" → цвет; больще ничё, работать с нею легко как с клизмой
public sealed class ConnectionStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status.ToLowerInvariant() switch
            {
                "success" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5CFCB0")),
                "error" or "failed" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FC5C7C")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B8D9E")),
            };
        }
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B8D9E"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
