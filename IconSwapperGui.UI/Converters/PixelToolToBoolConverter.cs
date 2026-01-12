using IconSwapperGui.Core.PixelArt;
using System;
using System.Globalization;
using System.Windows.Data;

namespace IconSwapperGui.UI.Converters;

public sealed class PixelToolToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not PixelTool current) return false;
        if (parameter is null) return false;

        return string.Equals(current.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool isChecked || !isChecked) return Binding.DoNothing;
        if (parameter is null) return Binding.DoNothing;

        if (Enum.TryParse(typeof(PixelTool), parameter.ToString(), ignoreCase: true, out var tool) && tool is PixelTool typed)
        {
            return typed;
        }

        return Binding.DoNothing;
    }
}
