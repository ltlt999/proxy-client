using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProxyClient.Converters;

public class LatencyToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        if (text.Contains("ms"))
        {
            if (int.TryParse(text.Replace("ms", "").Trim(), out var ms))
            {
                if (ms < 200) return new SolidColorBrush(Color.FromRgb(94, 201, 140));
                if (ms < 500) return new SolidColorBrush(Color.FromRgb(232, 196, 107));
                if (ms < 1000) return new SolidColorBrush(Color.FromRgb(239, 124, 124));
            }
        }
        if (text is "超时" or "timeout" or "Timeout") return new SolidColorBrush(Color.FromRgb(126, 128, 156));
        if (text is "测试中…") return new SolidColorBrush(Color.FromRgb(124, 156, 255));
        return new SolidColorBrush(Color.FromRgb(126, 128, 156));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class LatencyToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        if (text.Contains("ms") || text is "超时" or "测试中…")
            return new SolidColorBrush(Colors.White);
        return new SolidColorBrush(Color.FromRgb(126, 128, 156));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToActiveBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool active && active)
            return new SolidColorBrush(Color.FromRgb(124, 156, 255));
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
