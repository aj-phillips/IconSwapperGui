using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IconSwapperGui.UI.Converters;

public sealed class SectionVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return Visibility.Collapsed;

        var selectedSection = value.ToString();
        var targetSection = parameter.ToString();

        return string.Equals(selectedSection, targetSection, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}