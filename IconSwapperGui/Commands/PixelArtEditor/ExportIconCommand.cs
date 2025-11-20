using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IconSwapperGui.Helpers;
using IconSwapperGui.ViewModels;
using ImageMagick;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace IconSwapperGui.Commands.PixelArtEditor;

public class ExportIconCommand : RelayCommand
{
    private readonly PixelArtEditorViewModel _viewModel;
    private double _canvasDpiX;
    private double _canvasDpiY;

    public ExportIconCommand(PixelArtEditorViewModel viewModel, Action<object> execute,
        Func<object, bool>? canExecute = null) : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        var defaultName = string.IsNullOrWhiteSpace(_viewModel.IconName) ? "Pixel_Art" : _viewModel.IconName;
        var dlg = new Windows.ExportDialogWindow(defaultName) { Owner = System.Windows.Application.Current.MainWindow };
        var res = dlg.ShowDialog();
        if (res != true) return;

        var folderPath = dlg.FolderPath;
        var fileName = dlg.FileName?.Trim();
        if (string.IsNullOrEmpty(folderPath) || string.IsNullOrEmpty(fileName)) return;

        _viewModel.IconName = fileName;

        CanvasHelper.GetDpi(_viewModel.DrawableCanvas, out _canvasDpiX, out _canvasDpiY);

        var pngPath = SaveCanvasAsPngForExportTo(folderPath, fileName);
        if (string.IsNullOrEmpty(pngPath)) return;

        CreateAndSaveIconFromPng(pngPath, folderPath, fileName);

        DeleteTemporaryFile(pngPath);
    }

    private static string OpenFolderDialog()
    {
        var openFolderDialog = new OpenFolderDialog();

        return openFolderDialog.ShowDialog() == true ? openFolderDialog.FolderName : string.Empty;
    }

    private string SaveCanvasAsPngForExportTo(string folderPath, string baseFileName)
    {
        if (_viewModel.DrawableCanvas == null) return string.Empty;

        var exportCanvas = new Canvas
        {
            Width = _viewModel.DrawableCanvas.ActualWidth,
            Height = _viewModel.DrawableCanvas.ActualHeight,
            Background = new SolidColorBrush(_viewModel.BackgroundColor)
        };

        foreach (var pixelRect in _viewModel.Pixels)
        {
            var exportRect = new Rectangle
            {
                Width = pixelRect.Width,
                Height = pixelRect.Height,
                Fill = pixelRect.Fill,
                Stroke = pixelRect.Stroke,
                StrokeThickness = 0
            };

            Canvas.SetLeft(exportRect, Canvas.GetLeft(pixelRect));
            Canvas.SetTop(exportRect, Canvas.GetTop(pixelRect));

            exportCanvas.Children.Add(exportRect);
        }

        return SaveCanvasAsPngWithName(exportCanvas, folderPath, baseFileName);
    }

    private string SaveCanvasAsPngWithName(FrameworkElement canvas, string folderPath, string baseFileName)
    {
        var width = (int)canvas.Width;
        var height = (int)canvas.Height;

        var renderBitmap = new RenderTargetBitmap(width, height, _canvasDpiX, _canvasDpiY, PixelFormats.Pbgra32);

        canvas.Measure(new Size(width, height));
        canvas.Arrange(new Rect(new Size(width, height)));
        renderBitmap.Render(canvas);

        var safeBase = GetSafeBaseNameFor(baseFileName);
        var pngPath = Path.Combine(folderPath, $"{safeBase}.png");

        try
        {
            using var fileStream = new FileStream(pngPath, FileMode.Create);
            var encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            encoder.Save(fileStream);
        }
        catch (Exception)
        {
            return string.Empty;
        }

        return pngPath;
    }

    private string SaveCanvasAsPngForExport(string folderPath)
    {
        if (_viewModel.DrawableCanvas == null) return string.Empty;

        var exportCanvas = new Canvas
        {
            Width = _viewModel.DrawableCanvas.ActualWidth,
            Height = _viewModel.DrawableCanvas.ActualHeight,
            Background = new SolidColorBrush(_viewModel.BackgroundColor)
        };

        foreach (var pixelRect in _viewModel.Pixels)
        {
            var exportRect = new Rectangle
            {
                Width = pixelRect.Width,
                Height = pixelRect.Height,
                Fill = pixelRect.Fill,
                Stroke = pixelRect.Stroke,
                StrokeThickness = 0
            };

            Canvas.SetLeft(exportRect, Canvas.GetLeft(pixelRect));
            Canvas.SetTop(exportRect, Canvas.GetTop(pixelRect));

            exportCanvas.Children.Add(exportRect);
        }

        return SaveCanvasAsPng(exportCanvas, folderPath);
    }

    private string SaveCanvasAsPng(FrameworkElement canvas, string folderPath)
    {
        var width = (int)canvas.Width;
        var height = (int)canvas.Height;

        var renderBitmap = new RenderTargetBitmap(width, height, _canvasDpiX, _canvasDpiY, PixelFormats.Pbgra32);

        canvas.Measure(new Size(width, height));
        canvas.Arrange(new Rect(new Size(width, height)));
        renderBitmap.Render(canvas);

        var baseName = GetSafeBaseName();
        var pngPath = Path.Combine(folderPath, $"{baseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");

        try
        {
            using var fileStream = new FileStream(pngPath, FileMode.Create);
            var encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            encoder.Save(fileStream);
        }
        catch (Exception)
        {
            return string.Empty;
        }

        return pngPath;
    }

    private void CreateAndSaveIconFromPng(string pngPath, string folderPath, string baseFileName)
    {
        using var image = new MagickImage(pngPath);
        image.Alpha(AlphaOption.On);

        const int targetSize = 128;
        image.Resize(targetSize, targetSize);

        using var finalImage = new MagickImage(MagickColors.Transparent, targetSize, targetSize);
        finalImage.Alpha(AlphaOption.On);
        var offsetX = (int)(targetSize - image.Width) / 2;
        var offsetY = (int)(targetSize - image.Height) / 2;

        finalImage.Composite(image, offsetX, offsetY, CompositeOperator.Over);

        var safeBase = GetSafeBaseNameFor(baseFileName);
        var icoPath = Path.Combine(folderPath, $"{safeBase}.ico");
        finalImage.Format = MagickFormat.Icon;
        finalImage.Write(icoPath);
    }

    private static string GetSafeBaseNameFor(string name)
    {
        var baseName = name ?? string.Empty;
        baseName = baseName.Trim();
        if (string.IsNullOrEmpty(baseName)) baseName = "Pixel_Art";

        baseName = Path.GetInvalidFileNameChars().Aggregate(baseName, (current, c) => current.Replace(c, '_'));

        while (baseName.Contains("__")) baseName = baseName.Replace("__", "_");

        return baseName;
    }

    private string GetSafeBaseName()
    {
        var name = _viewModel.IconName ?? string.Empty;
        name = name.Trim();
        if (string.IsNullOrEmpty(name)) name = "Pixel_Art";

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        // Collapse multiple underscores
        while (name.Contains("__")) name = name.Replace("__", "_");

        return name;
    }

    private static void DeleteTemporaryFile(string filePath)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}