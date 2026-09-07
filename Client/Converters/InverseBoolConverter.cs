using System.Globalization;
using System.Windows.Data;

namespace AiDesktopClient.Converters;

// зеркалит bool туда-обратно; нужен для коллапса АККУРАТНО - перепутаешь, UI спрячет по логике-иронии
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return true;
    }
}
