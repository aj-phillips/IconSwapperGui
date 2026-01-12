using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IconSwapperGui.UI.Converters;

public sealed class TruncateStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var text = value.ToString() ?? string.Empty;
        if (text.Length == 0)
        {
            return string.Empty;
        }

        var maxLength = 0;
        if (parameter is int p)
        {
            maxLength = p;
        }
        else if (parameter is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            maxLength = parsed;
        }

        if (maxLength <= 0)
        {
            return text;
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength] + "…";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
