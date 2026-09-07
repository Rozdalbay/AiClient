using System.Globalization;
using System.Windows.Data;

namespace AiDesktopClient.Converters;

public sealed class NullableChangePercentToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            var arrow = d >= 0 ? "\u2191" : "\u2193";
            return $"{arrow} {Math.Abs(d):F0}%";
        }
        return "\u2014";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class NullableChangePercentToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d >= 0 ? "#5CFCB0" : "#FC5C7C";
        return "#5C5E6E";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class PercentToScaleXConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return Math.Clamp((double)d / 100.0, 0.0, 1.0);
        if (value is double db)
            return Math.Clamp(db / 100.0, 0.0, 1.0);
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class InitialConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && s.Length > 0)
            return s[..1].ToUpperInvariant();
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class CompactTokenCountConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long l)
        {
            if (l >= 1_000_000) return $"{l / 1_000_000.0:0.#}M";
            return l.ToString("N0");
        }
        if (value is int i)
        {
            var n = (long)i;
            if (n >= 1_000_000) return $"{n / 1_000_000.0:0.#}M";
            return n.ToString("N0");
        }
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}