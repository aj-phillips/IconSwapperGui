using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.UI.Converters;

public class NotificationTypeToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NotificationType type)
            return type switch
            {
                NotificationType.Success => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                NotificationType.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B")),
                NotificationType.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                NotificationType.Info => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"))
            };

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}