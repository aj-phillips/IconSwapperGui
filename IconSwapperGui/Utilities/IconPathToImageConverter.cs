using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace IconSwapperGui.Utilities;

[ValueConversion(typeof(string), typeof(BitmapImage))]
public class IconPathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var path = value as string;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        using var icon = Icon.ExtractAssociatedIcon(path);

        using var bmp = icon?.ToBitmap();
        
        var stream = new MemoryStream();
        
        bmp?.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        
        stream.Position = 0;
        
        var bitmapImage = new BitmapImage();
        
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = stream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();
        
        return bitmapImage;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}