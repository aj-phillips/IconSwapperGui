using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IconSwapperGui.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            var invert = parameter?.ToString() == "Invert";
            var result = invert ? !boolValue : boolValue;
            return result ? Visibility.Visible : Visibility.Collapsed;
        }

        if (value is not int intValue) return Visibility.Collapsed;
        {
            var invert = parameter?.ToString() == "Invert";
            var result = intValue > 0;
            result = invert ? !result : result;
            return result ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}