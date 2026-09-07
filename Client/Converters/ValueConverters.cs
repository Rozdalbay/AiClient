using System.Globalization;
using System.Windows.Data;

namespace AiDesktopClient.Converters;

// мелочёвка: on/off для opacity и проценты, читай и не трогай лишнего - каждая утка на своём месте повол
public sealed class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? 1.0 : 0.4;
        return 0.4;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class DoubleToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return $"{d * 100:F0}%";
        return "0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class DecimalToCurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            if (d == 0) return "$0.00";
            if (Math.Abs(d) < 0.01m) return $"${d:F4}";
            if (Math.Abs(d) < 1m) return $"${d:F3}";
            if (Math.Abs(d) >= 1_000_000m) return $"${d / 1_000_000m:F2}M";
            if (Math.Abs(d) >= 1_000m) return $"${d / 1_000m:F1}K";
            return $"${d:F2}";
        }
        return "$0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class IntToFormattedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int i)
            return i.ToString("N0");
        if (value is long l)
            return l.ToString("N0");
        return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class BackendStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Contracts.BackendStatus status)
        {
            return status switch
            {
                Contracts.BackendStatus.Connected => "#5CFCB0",
                Contracts.BackendStatus.Connecting => "#FCE55C",
                Contracts.BackendStatus.Disconnected => "#FC5C7C",
                Contracts.BackendStatus.Error => "#FC5C7C",
                _ => "#5C5E6E"
            };
        }
        return "#5C5E6E";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class BackendStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Contracts.BackendStatus status)
        {
            return status switch
            {
                Contracts.BackendStatus.Connected => "Connected",
                Contracts.BackendStatus.Connecting => "Connecting...",
                Contracts.BackendStatus.Disconnected => "Disconnected",
                Contracts.BackendStatus.Error => "Error",
                _ => "Unknown"
            };
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class ChangePercentToDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            var arrow = d >= 0 ? "\u2191" : "\u2193";
            return $"{arrow} {Math.Abs(d):F0}%";
        }
        return "\u2191 0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class ChangePercentToColorConverter : IValueConverter
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

public sealed class DateTimeToRelativeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return dt.ToString("MMM d");
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class MessageRoleToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Models.MessageRole role)
            return role == Models.MessageRole.User
                ? System.Windows.HorizontalAlignment.Right
                : System.Windows.HorizontalAlignment.Left;
        return System.Windows.HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class MessageRoleToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Models.MessageRole role)
            return role == Models.MessageRole.User
                ? System.Windows.Application.Current.FindResource("PrimaryAccentBrush")
                : System.Windows.Application.Current.FindResource("TertiaryBackgroundBrush");
        return System.Windows.Application.Current.FindResource("TertiaryBackgroundBrush");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
