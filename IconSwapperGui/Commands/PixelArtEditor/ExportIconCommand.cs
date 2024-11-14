using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IconSwapperGui.Helpers;
using IconSwapperGui.ViewModels;
using ImageMagick;
using Microsoft.Win32;

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
        var canvas = _viewModel.DrawableCanvas;

        if (canvas == null) return;

        var folderPath = OpenFolderDialog();
        if (string.IsNullOrEmpty(folderPath)) return;

        CanvasHelper.GetDpi(canvas, out _canvasDpiX, out _canvasDpiY);

        var pngPath = SaveCanvasAsPng(canvas, folderPath);
        if (string.IsNullOrEmpty(pngPath)) return;

        CreateAndSaveIcon(pngPath, folderPath);

        DeleteTemporaryFile(pngPath);
    }

    private static string OpenFolderDialog()
    {
        var openFolderDialog = new OpenFolderDialog();
        
        return openFolderDialog.ShowDialog() == true ? openFolderDialog.FolderName : string.Empty;
    }

    private string SaveCanvasAsPng(FrameworkElement canvas, string folderPath)
    {
        var width = (int)canvas.ActualWidth;
        var height = (int)canvas.ActualHeight;

        var renderBitmap = new RenderTargetBitmap(width, height, _canvasDpiX, _canvasDpiY, PixelFormats.Pbgra32);

        canvas.Measure(new Size(width, height));
        canvas.Arrange(new Rect(new Size(width, height)));
        renderBitmap.Render(canvas);

        var pngPath = Path.Combine(folderPath, $"Full_Canvas_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");

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

    private void CreateAndSaveIcon(string pngPath, string folderPath)
    {
        using var image = new MagickImage(pngPath);

        const int targetSize = 128;
        image.Resize(targetSize, targetSize);

        using var finalImage = new MagickImage(MagickColors.Transparent, targetSize, targetSize);
        var offsetX = (int)(targetSize - image.Width) / 2;
        var offsetY = (int)(targetSize - image.Height) / 2;

        finalImage.Composite(image, offsetX, offsetY, CompositeOperator.Over);

        var icoPath = Path.Combine(folderPath, $"Pixel_Art_{DateTime.UtcNow:yyyyMMdd_HHmmss}.ico");
        finalImage.Format = MagickFormat.Icon;
        finalImage.Write(icoPath);
    }

    private static void DeleteTemporaryFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}