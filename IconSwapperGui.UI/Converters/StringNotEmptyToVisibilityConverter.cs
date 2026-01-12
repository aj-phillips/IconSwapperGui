using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IconSwapperGui.UI.Converters;

public class StringNotEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = value as string;
        var isEmpty = string.IsNullOrWhiteSpace(str);
        return isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
