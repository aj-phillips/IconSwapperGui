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

    public void ExportFolderTheme(string outputPath, FolderThemeRenderOptions options)
    {
        if (string.IsNullOrWhiteSpace(outputPath)) throw new InvalidOperationException("Output path is required.");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
        var pngPath = Path.ChangeExtension(outputPath, ".png");
        var bitmap = RenderFolderThemeBitmap(256, options);
        SavePng(bitmap, pngPath);
        _iconCreatorService.CreateMultiSizeIcoFromImage(pngPath, outputPath, [16, 32, 48, 64, 128, 256],
            new ImageTransformOptions { CropMode = ImageCropMode.FitWithPadding });
    }

    public BitmapSource RenderFolderThemeBitmap(
        int size,
        Color folderColor,
        Color accentColor,
        string badge,
        string label,
        bool showBadge = true,
        double labelFontSize = 24)
        => RenderFolderThemeBitmap(size, new FolderThemeRenderOptions
        {
            FolderColor = folderColor,
            AccentColor = accentColor,
            Badge = badge,
            Label = label,
            ShowBadge = showBadge,
            LabelFontSize = labelFontSize
        });

    public BitmapSource RenderFolderThemeBitmap(int size, FolderThemeRenderOptions options)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            DrawWindowsFolder(context, size, options);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void DrawWindowsFolder(DrawingContext context, int size, FolderThemeRenderOptions options)
    {
        var depth = Math.Clamp(options.Depth, 0.0, 1.0);
        var radius = Math.Clamp(options.CornerSoftness, 4.0, 22.0) / 256.0 * size;
        var folderColor = options.FolderColor;
        var accentColor = options.AccentColor;
        var shadowOpacity = (byte)(24 + depth * 42);

        var folderDark = Darken(folderColor, 0.76);
        var folderMid = folderColor;
        var folderLight = Lighten(folderColor, 1.22);
        var folderFront = Lighten(folderColor, 1.10);
        var borderPen = new Pen(new SolidColorBrush(Darken(folderColor, 0.72)), Math.Max(1, size * 0.008));

        context.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromArgb(shadowOpacity, 0, 0, 0)),
            null,
            new System.Windows.Rect(size * 0.13, size * 0.37, size * 0.76, size * 0.50),
            radius,
            radius);

        DrawFolderBack(context, size, options, folderDark, folderMid, folderLight, radius, borderPen);

        if (options.ShowDocument)
            DrawDocumentInsert(context, size, options);

        DrawFolderFront(context, size, options, folderFront, radius, borderPen);

        if (string.Equals(options.StyleName, "Glass", StringComparison.OrdinalIgnoreCase))
            DrawGlassOverlay(context, size, accentColor, radius);
        else if (string.Equals(options.StyleName, "Project", StringComparison.OrdinalIgnoreCase))
            DrawProjectStripe(context, size, accentColor);
        else if (string.Equals(options.StyleName, "Archive", StringComparison.OrdinalIgnoreCase))
            DrawArchiveLines(context, size, folderColor);

        DrawLabel(context, size, options);
        DrawBadge(context, size, options);
    }

    private static void DrawFolderBack(
        DrawingContext context,
        int size,
        FolderThemeRenderOptions options,
        Color folderDark,
        Color folderMid,
        Color folderLight,
        double radius,
        Pen borderPen)
    {
        var backBrush = new LinearGradientBrush(
            Lighten(folderMid, 1.12),
            folderMid,
            new System.Windows.Point(0.5, 0.22),
            new System.Windows.Point(0.5, 0.82));

        var backGeometry = CreateBackFolderGeometry(size, options.TabStyleName, radius);
        context.DrawGeometry(
            backBrush,
            borderPen,
            backGeometry);

        context.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
            null,
            new System.Windows.Rect(size * 0.15, size * 0.37, size * 0.70, size * 0.05));

        context.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromArgb(70, folderLight.R, folderLight.G, folderLight.B)),
            null,
            new System.Windows.Rect(size * 0.14, size * 0.35, size * 0.73, size * 0.17),
            radius * 0.65,
            radius * 0.65);
    }

    private static Geometry CreateBackFolderGeometry(int size, string tabStyleName, double radius)
    {
        var geometry = new StreamGeometry();
        using var stream = geometry.Open();

        var angled = string.Equals(tabStyleName, "Angled", StringComparison.OrdinalIgnoreCase);
        var r = Math.Max(size * 0.018, radius * 0.5);
        var left = size * 0.10;
        var top = size * 0.30;
        var right = size * 0.91;
        var bottom = size * 0.79;
        var tabLeft = size * 0.15;
        var tabTop = size * 0.19;
        var tabRight = size * (angled ? 0.50 : 0.45);

        stream.BeginFigure(new System.Windows.Point(left + r, top), true, true);
        stream.LineTo(new System.Windows.Point(tabLeft, top), true, false);
        stream.LineTo(new System.Windows.Point(tabLeft, tabTop + r), true, false);
        stream.QuadraticBezierTo(
            new System.Windows.Point(tabLeft, tabTop),
            new System.Windows.Point(tabLeft + r, tabTop),
            true,
            false);
        stream.LineTo(new System.Windows.Point(size * 0.39, tabTop), true, false);
        stream.LineTo(new System.Windows.Point(tabRight, top), true, false);
        stream.LineTo(new System.Windows.Point(right - r, top), true, false);
        stream.QuadraticBezierTo(new System.Windows.Point(right, top), new System.Windows.Point(right, top + r), true, false);
        stream.LineTo(new System.Windows.Point(right, bottom - r), true, false);
        stream.QuadraticBezierTo(new System.Windows.Point(right, bottom), new System.Windows.Point(right - r, bottom), true, false);
        stream.LineTo(new System.Windows.Point(left + r, bottom), true, false);
        stream.QuadraticBezierTo(new System.Windows.Point(left, bottom), new System.Windows.Point(left, bottom - r), true, false);
        stream.LineTo(new System.Windows.Point(left, top + r), true, false);
        stream.QuadraticBezierTo(new System.Windows.Point(left, top), new System.Windows.Point(left + r, top), true, false);

        geometry.Freeze();
        return geometry;
    }

    private static void DrawDocumentInsert(DrawingContext context, int size, FolderThemeRenderOptions options)
    {
        var paper = new SolidColorBrush(Color.FromRgb(252, 252, 247));
        var paperBorder = new Pen(new SolidColorBrush(Color.FromRgb(210, 214, 222)), Math.Max(1, size * 0.004));
        var x = size * 0.44;
        var y = size * 0.29;
        var width = size * 0.22;
        var height = size * 0.30;

        context.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(28, 0, 0, 0)),
            null,
            new System.Windows.Rect(x + size * 0.015, y + size * 0.018, width, height));
        context.DrawRectangle(paper, paperBorder, new System.Windows.Rect(x, y, width, height));

        var fold = new StreamGeometry();
        using (var stream = fold.Open())
        {
            stream.BeginFigure(new System.Windows.Point(x + width * 0.66, y), true, true);
            stream.LineTo(new System.Windows.Point(x + width, y + height * 0.26), true, false);
            stream.LineTo(new System.Windows.Point(x + width * 0.66, y + height * 0.26), true, false);
        }

        context.DrawGeometry(new SolidColorBrush(Color.FromRgb(238, 240, 244)), paperBorder, fold);

        var linePen = new Pen(new SolidColorBrush(Color.FromRgb(205, 213, 224)), Math.Max(1, size * 0.003));
        for (var i = 0; i < 7; i++)
        {
            var lineY = y + height * (0.35 + i * 0.075);
            context.DrawLine(linePen, new System.Windows.Point(x + width * 0.14, lineY), new System.Windows.Point(x + width * 0.82, lineY));
        }

        if (string.Equals(options.StyleName, "Project", StringComparison.OrdinalIgnoreCase))
        {
            var accentPen = new Pen(new SolidColorBrush(options.AccentColor), Math.Max(1, size * 0.01));
            context.DrawLine(accentPen, new System.Windows.Point(x + width * 0.14, y + height * 0.82), new System.Windows.Point(x + width * 0.70, y + height * 0.82));
        }
    }

    private static void DrawFolderFront(
        DrawingContext context,
        int size,
        FolderThemeRenderOptions options,
        Color folderFront,
        double radius,
        Pen borderPen)
    {
        var frontBrush = new LinearGradientBrush(
            Lighten(folderFront, 1.13),
            folderFront,
            new System.Windows.Point(0.5, 0.46),
            new System.Windows.Point(0.5, 0.90));

        var frontRect = new System.Windows.Rect(size * 0.09, size * 0.45, size * 0.82, size * 0.37);
        context.DrawRoundedRectangle(frontBrush, borderPen, frontRect, radius, radius);

        context.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromArgb(72, 255, 255, 255)),
            null,
            new System.Windows.Rect(size * 0.13, size * 0.49, size * 0.74, size * 0.08),
            radius * 0.6,
            radius * 0.6);
    }

    private static void DrawGlassOverlay(DrawingContext context, int size, Color accentColor, double radius)
    {
        context.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromArgb(58, accentColor.R, accentColor.G, accentColor.B)),
            null,
            new System.Windows.Rect(size * 0.10, size * 0.46, size * 0.80, size * 0.34),
            radius,
            radius);
    }

    private static void DrawProjectStripe(DrawingContext context, int size, Color accentColor)
    {
        context.DrawRoundedRectangle(
            new SolidColorBrush(accentColor),
            null,
            new System.Windows.Rect(size * 0.15, size * 0.68, size * 0.34, size * 0.035),
            size * 0.017,
            size * 0.017);
    }

    private static void DrawArchiveLines(DrawingContext context, int size, Color folderColor)
    {
        var pen = new Pen(new SolidColorBrush(Darken(folderColor, 0.72)), Math.Max(1, size * 0.01));
        for (var i = 0; i < 3; i++)
        {
            var y = size * (0.55 + i * 0.07);
            context.DrawLine(pen, new System.Windows.Point(size * 0.18, y), new System.Windows.Point(size * 0.35, y));
        }
    }

    private static void DrawBadge(DrawingContext context, int size, FolderThemeRenderOptions options)
    {
        if (!options.ShowBadge || string.IsNullOrWhiteSpace(options.Badge))
            return;

        var badgeRect = new System.Windows.Rect(size * 0.66, size * 0.57, size * 0.17, size * 0.17);
        context.DrawRoundedRectangle(new SolidColorBrush(options.AccentColor), null, badgeRect, badgeRect.Width / 2, badgeRect.Height / 2);

        var badgeText = new FormattedText(
            options.Badge[..Math.Min(2, options.Badge.Length)].ToUpperInvariant(),
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            size * 0.09,
            Brushes.White,
            1.0);

        context.DrawText(badgeText, new System.Windows.Point(
            badgeRect.Left + badgeRect.Width / 2 - badgeText.Width / 2,
            badgeRect.Top + badgeRect.Height / 2 - badgeText.Height / 2));
    }

    private static void DrawLabel(DrawingContext context, int size, FolderThemeRenderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Label) ||
            string.Equals(options.LabelPlacementName, "None", StringComparison.OrdinalIgnoreCase))
            return;

        var labelText = new FormattedText(
            options.Label[..Math.Min(14, options.Label.Length)],
            System.Globalization.CultureInfo.CurrentCulture,
            System.Windows.FlowDirection.LeftToRight,
            new Typeface("Segoe UI Semibold"),
            Math.Clamp(options.LabelFontSize, 12, 48) / 256.0 * size,
            new SolidColorBrush(Darken(options.FolderColor, 0.45)),
            1.0);

        labelText.MaxTextWidth = size * 0.62;
        var y = string.Equals(options.LabelPlacementName, "Tab", StringComparison.OrdinalIgnoreCase)
            ? size * 0.31
            : size * 0.58;

        context.DrawText(labelText, new System.Windows.Point(size * 0.18, y));
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

    private static Color Lighten(Color color, double factor)
        => Color.FromArgb(color.A, ClampColor(color.R * factor), ClampColor(color.G * factor), ClampColor(color.B * factor));

    private static byte ClampColor(double value)
        => (byte)Math.Clamp(value, 0, 255);
}
