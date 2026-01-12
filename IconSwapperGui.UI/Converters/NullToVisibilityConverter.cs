using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IconSwapperGui.UI.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isNull = value == null;
        var result = Invert ? !isNull : isNull;
        return result ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
