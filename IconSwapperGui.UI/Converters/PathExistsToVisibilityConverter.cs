using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace IconSwapperGui.UI.Converters;

public sealed class PathExistsToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var path = value as string;
        var exists = !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
        if (Invert)
            exists = !exists;

        return exists ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
