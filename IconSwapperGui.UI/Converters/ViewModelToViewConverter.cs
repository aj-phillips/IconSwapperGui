using System.Globalization;
using System.Windows.Data;
using IconSwapperGui.UI.ViewModels;
using IconSwapperGui.UI.Views;

namespace IconSwapperGui.UI.Converters;

public class ViewModelToViewConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;

        return value switch
        {
            SwapperViewModel vm => new SwapperView { DataContext = vm },
            ConverterViewModel vm => new ConverterView { DataContext = vm },
            PixelArtEditorViewModel vm => new PixelArtEditorView { DataContext = vm },
            SettingsViewModel vm => new SettingsView { DataContext = vm },
            _ => null
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}