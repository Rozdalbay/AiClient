using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AiDesktopClient.Converters;

// hex-строка → кисть; если мусор в строке - прозрачный, чтобы не уронить рендер
public sealed class StringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorStr)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStr));
            }
            catch
            {
                return Brushes.Transparent;
            }
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
