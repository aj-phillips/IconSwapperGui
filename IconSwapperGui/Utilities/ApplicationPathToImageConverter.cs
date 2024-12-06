using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Shell;

namespace IconSwapperGui.Utilities;

[ValueConversion(typeof(string), typeof(BitmapImage))]
public class ApplicationPathToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var path = value as string;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;

        var shellFile = ShellFile.FromFilePath(path);
        var shellThumbnail = shellFile.Thumbnail?.ExtraLargeBitmapSource;

        if (shellThumbnail == null)
            return null;

        var bitmapImage = new BitmapImage();
        var stream = new MemoryStream();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(shellThumbnail));
        encoder.Save(stream);

        bitmapImage.BeginInit();
        bitmapImage.StreamSource = new MemoryStream(stream.ToArray());
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