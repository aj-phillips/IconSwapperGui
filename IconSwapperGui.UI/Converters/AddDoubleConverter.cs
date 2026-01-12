using System;
using System.Globalization;
using System.Windows.Data;

namespace IconSwapperGui.UI.Converters;

public sealed class AddDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double d) return value;

        var add = 0d;
        if (parameter is not null)
        {
            _ = double.TryParse(parameter.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out add);
        }

        return d + add;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
