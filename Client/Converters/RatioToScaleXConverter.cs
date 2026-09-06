using System.Globalization;
using System.Windows.Data;

namespace AiDesktopClient.Converters;

public sealed class RatioToScaleXConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2 && values[0] is double dailyCost && values[1] is double budgetLimit)
        {
            if (budgetLimit <= 0) return 0.0;
            return Math.Clamp(dailyCost / budgetLimit, 0.0, 1.0);
        }
        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
