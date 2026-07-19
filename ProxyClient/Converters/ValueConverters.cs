using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProxyClient.Converters;

// Tinted iOS-style latency pills: light tinted background + dark readable foreground.
public class LatencyToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        if (text.Contains("ms"))
        {
            if (int.TryParse(text.Replace("ms", "").Trim(), out var ms))
            {
                if (ms < 200) return new SolidColorBrush(Color.FromRgb(0xE5, 0xF8, 0xEB));
                if (ms < 500) return new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xE2));
                return new SolidColorBrush(Color.FromRgb(0xFD, 0xEC, 0xEA));
            }
        }
        if (text is "超时" or "timeout" or "Timeout") return new SolidColorBrush(Color.FromRgb(0xEF, 0xEF, 0xF1));
        if (text is "测试中…") return new SolidColorBrush(Color.FromRgb(0xE5, 0xF0, 0xFF));
        return new SolidColorBrush(Color.FromRgb(0xF1, 0xF1, 0xF4));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class LatencyToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        if (text.Contains("ms"))
        {
            if (int.TryParse(text.Replace("ms", "").Trim(), out var ms))
            {
                if (ms < 200) return new SolidColorBrush(Color.FromRgb(0x15, 0x7A, 0x38));
                if (ms < 500) return new SolidColorBrush(Color.FromRgb(0x8A, 0x5A, 0x00));
                return new SolidColorBrush(Color.FromRgb(0xB3, 0x27, 0x1E));
            }
            return new SolidColorBrush(Color.FromRgb(0xB3, 0x27, 0x1E));
        }
        if (text is "超时" or "timeout" or "Timeout") return new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x6A));
        if (text is "测试中…") return new SolidColorBrush(Color.FromRgb(0x00, 0x57, 0xC2));
        return new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x7E));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToActiveBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool active && active)
            return new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xFF));
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}
