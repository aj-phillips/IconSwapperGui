using System.Globalization;
using System.Windows.Data;

namespace IconSwapperGui.UI.Converters;

public sealed class WidthToLayoutConverter : IValueConverter
{
    // Returns "Small" when width is below 600, otherwise "Normal". Simple heuristic.
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d) return d < 600 ? "Small" : "Normal";

        return "Normal";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}