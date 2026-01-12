using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IconSwapperGui.UI.Converters;

public sealed class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Color c) return Brushes.Transparent;

        var brush = new SolidColorBrush(c);
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
