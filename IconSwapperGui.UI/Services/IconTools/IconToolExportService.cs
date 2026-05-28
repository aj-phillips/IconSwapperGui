using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Images;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IconSwapperGui.UI.Services.IconTools;

public sealed class IconToolExportService
{
    private readonly IIconCreatorService _iconCreatorService;

    public IconToolExportService(IIconCreatorService iconCreatorService)
    {
        _iconCreatorService = iconCreatorService;
    }

    public void NormalizeImage(string sourcePath, string outputFolder, int size, ImageTransformOptions options, bool exportPng, bool exportIco)
    {
        if (string.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Source image not found.", sourcePath);
        if (string.IsNullOrWhiteSpace(outputFolder)) throw new InvalidOperationException("Output folder is required.");
        if (size <= 0) throw new InvalidOperationException("Output size must be greater than zero.");
        if (!exportPng && !exportIco) throw new InvalidOperationException("Choose at least one output format.");

        Directory.CreateDirectory(outputFolder);
        var safeName = GetSafeBaseName(Path.GetFileNameWithoutExtension(sourcePath));
        var pngPath = Path.Combine(outputFolder, $"{safeName}_{size}.png");
        var icoPath = Path.Combine(outputFolder, $"{safeName}_{size}.ico");

        if (exportPng)
        {
            var bitmap = RenderNormalizedBitmap(sourcePath, size, options);
            SavePng(bitmap, pngPath);
        }

        if (exportIco)
        {
            _iconCreatorService.CreateMultiSizeIcoFromImage(sourcePath, icoPath, [16, 32, 48, 64, 128, 256], options);
        }
    }

    public void ExportFolderTheme(string outputPath, Color folderColor, Color accentColor, string badge, string label)
    {
        if (string.IsNullOrWhiteSpace(outputPath)) throw new InvalidOperationException("Output path is required.");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        var pngPath = Path.ChangeExtension(outputPath, ".png");
        var bitmap = RenderFolderThemeBitmap(256, folderColor, accentColor, badge, label);
        SavePng(bitmap, pngPath);
        _iconCreatorService.CreateMultiSizeIcoFromImage(pngPath, outputPath, [16, 32, 48, 64, 128, 256],
            new ImageTransformOptions { CropMode = ImageCropMode.FitWithPadding });
    }

    public BitmapSource RenderFolderThemeBitmap(int size, Color folderColor, Color accentColor, string badge, string label)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var shadow = new SolidColorBrush(Color.FromArgb(45, 0, 0, 0));
            var folderBrush = new SolidColorBrush(folderColor);
            var folderDark = new SolidColorBrush(Darken(folderColor, 0.82));
            var accentBrush = new SolidColorBrush(accentColor);
            var white = Brushes.White;

            context.DrawRoundedRectangle(shadow, null, new System.Windows.Rect(size * 0.13, size * 0.28, size * 0.76, size * 0.56), size * 0.08, size * 0.08);
            context.DrawRoundedRectangle(folderDark, null, new System.Windows.Rect(size * 0.13, size * 0.22, size * 0.32, size * 0.18), size * 0.06, size * 0.06);
            context.DrawRoundedRectangle(folderBrush, null, new System.Windows.Rect(size * 0.10, size * 0.30, size * 0.80, size * 0.52), size * 0.08, size * 0.08);
            context.DrawRoundedRectangle(accentBrush, null, new System.Windows.Rect(size * 0.60, size * 0.50, size * 0.22, size * 0.22), size * 0.11, size * 0.11);

            if (!string.IsNullOrWhiteSpace(badge))
            {
                var badgeText = new FormattedText(
                    badge[..Math.Min(2, badge.Length)].ToUpperInvariant(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Segoe UI Semibold"),
                    size * 0.105,
                    white,
                    1.0);

                context.DrawText(badgeText, new System.Windows.Point(
                    size * 0.71 - badgeText.Width / 2,
                    size * 0.61 - badgeText.Height / 2));
            }

            if (!string.IsNullOrWhiteSpace(label))
            {
                var labelText = new FormattedText(
                    label[..Math.Min(10, label.Length)],
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Segoe UI Semibold"),
                    size * 0.085,
                    new SolidColorBrush(Darken(folderColor, 0.48)),
                    1.0);

                context.DrawText(labelText, new System.Windows.Point(
                    size * 0.18,
                    size * 0.40));
            }
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource RenderNormalizedBitmap(string sourcePath, int size, ImageTransformOptions options)
    {
        var decoder = BitmapDecoder.Create(new Uri(sourcePath), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var source = decoder.Frames.OrderByDescending(f => f.PixelWidth * f.PixelHeight).First();
        var sourceWidth = Math.Max(1, source.PixelWidth);
        var sourceHeight = Math.Max(1, source.PixelHeight);

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            if (!options.UseTransparentPadding)
                context.DrawRectangle(Brushes.White, null, new System.Windows.Rect(0, 0, size, size));

            var rect = GetDrawRect(sourceWidth, sourceHeight, size, options);
            context.PushClip(new RectangleGeometry(new System.Windows.Rect(0, 0, size, size)));
            context.DrawImage(source, rect);
            context.Pop();
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static System.Windows.Rect GetDrawRect(int sourceWidth, int sourceHeight, int size, ImageTransformOptions options)
    {
        var zoom = Math.Clamp(options.Zoom, 1.0, 4.0);
        var scale = options.CropMode == ImageCropMode.FitWithPadding
            ? Math.Min((double)size / sourceWidth, (double)size / sourceHeight)
            : Math.Max((double)size / sourceWidth, (double)size / sourceHeight);

        scale *= zoom;
        var width = sourceWidth * scale;
        var height = sourceHeight * scale;
        var maxOffsetX = Math.Max(0, width - size);
        var maxOffsetY = Math.Max(0, height - size);
        var left = width <= size ? (size - width) / 2 : -maxOffsetX * Math.Clamp(options.FocusX, 0, 1);
        var top = height <= size ? (size - height) / 2 : -maxOffsetY * Math.Clamp(options.FocusY, 0, 1);

        return new System.Windows.Rect(left, top, width, height);
    }

    private static void SavePng(BitmapSource bitmap, string path)
    {
        using var stream = File.Create(path);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
    }

    private static string GetSafeBaseName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return string.IsNullOrWhiteSpace(name) ? "icon" : name;
    }

    private static Color Darken(Color color, double factor)
        => Color.FromArgb(color.A, (byte)(color.R * factor), (byte)(color.G * factor), (byte)(color.B * factor));
}
