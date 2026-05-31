using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Models.Images;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IconSwapperGui.UI.ViewModels;

public partial class ImageCropEditorViewModel : ObservableObject
{
    private const int PreviewSize = 320;

    [ObservableProperty] private ImageCropMode _cropMode;
    [ObservableProperty] private bool _useTransparentPadding;
    [ObservableProperty] private double _zoom;
    [ObservableProperty] private double _focusX;
    [ObservableProperty] private double _focusY;
    [ObservableProperty] private BitmapSource? _previewImage;

    public ImageCropEditorViewModel(string imagePath, ImageTransformOptions options)
    {
        ImagePath = imagePath;
        FileName = Path.GetFileName(imagePath);
        SourceImage = LoadPreview(imagePath);
        CropMode = options.CropMode;
        UseTransparentPadding = options.UseTransparentPadding;
        Zoom = Math.Clamp(options.Zoom, 1.0, 4.0);
        FocusX = Math.Clamp(options.FocusX, 0.0, 1.0);
        FocusY = Math.Clamp(options.FocusY, 0.0, 1.0);
        UpdatePreview();
    }

    public string ImagePath { get; }
    public string FileName { get; }
    public BitmapSource? SourceImage { get; }

    public ImageTransformOptions ToOptions() => new()
    {
        CropMode = CropMode,
        UseTransparentPadding = UseTransparentPadding,
        Zoom = Zoom,
        FocusX = FocusX,
        FocusY = FocusY
    };

    public event Action<bool?>? RequestClose;

    public void PanFocus(double deltaX, double deltaY)
    {
        FocusX = Math.Clamp(FocusX - (deltaX / PreviewSize), 0.0, 1.0);
        FocusY = Math.Clamp(FocusY - (deltaY / PreviewSize), 0.0, 1.0);
    }

    public void AdjustZoom(double wheelDelta)
    {
        var direction = Math.Sign(wheelDelta);
        if (direction == 0)
            return;

        Zoom = Math.Clamp(Zoom + direction * 0.12, 1.0, 4.0);
    }

    partial void OnCropModeChanged(ImageCropMode value) => UpdatePreview();
    partial void OnUseTransparentPaddingChanged(bool value) => UpdatePreview();
    partial void OnZoomChanged(double value) => UpdatePreview();
    partial void OnFocusXChanged(double value) => UpdatePreview();
    partial void OnFocusYChanged(double value) => UpdatePreview();

    [RelayCommand]
    private void Apply()
    {
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    [RelayCommand]
    private void CenterFocus()
    {
        FocusX = 0.5;
        FocusY = 0.5;
    }

    private static BitmapSource? LoadPreview(string imagePath)
    {
        if (!File.Exists(imagePath))
            return null;

        var decoder = BitmapDecoder.Create(new Uri(imagePath), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames.OrderByDescending(f => f.PixelWidth * f.PixelHeight).FirstOrDefault();
        frame?.Freeze();
        return frame;
    }

    private void UpdatePreview()
    {
        if (SourceImage is null)
        {
            PreviewImage = null;
            return;
        }

        PreviewImage = CreatePreview(SourceImage, ToOptions());
    }

    private static BitmapSource CreatePreview(BitmapSource source, ImageTransformOptions options)
    {
        var sourceWidth = Math.Max(1, source.PixelWidth);
        var sourceHeight = Math.Max(1, source.PixelHeight);

        return options.CropMode switch
        {
            ImageCropMode.FitWithPadding => FitWithPaddingPreview(source, sourceWidth, sourceHeight, options),
            ImageCropMode.FillAndCrop => FillAndCropPreview(source, sourceWidth, sourceHeight, options),
            _ => CenterCropPreview(source, sourceWidth, sourceHeight, options)
        };
    }

    private static BitmapSource CenterCropPreview(BitmapSource source, int sourceWidth, int sourceHeight, ImageTransformOptions options)
    {
        var zoom = Math.Clamp(options.Zoom, 1.0, 4.0);
        var side = Math.Max(1, (int)Math.Round(Math.Min(sourceWidth, sourceHeight) / zoom));
        var crop = new CroppedBitmap(source, GetFocusedCrop(sourceWidth, sourceHeight, side, side, options));
        return Resize(crop, PreviewSize, PreviewSize);
    }

    private static BitmapSource FillAndCropPreview(BitmapSource source, int sourceWidth, int sourceHeight, ImageTransformOptions options)
    {
        var scale = Math.Max((double)PreviewSize / sourceWidth, (double)PreviewSize / sourceHeight) *
                    Math.Clamp(options.Zoom, 1.0, 4.0);
        var scaledWidth = Math.Max(PreviewSize, (int)Math.Round(sourceWidth * scale));
        var scaledHeight = Math.Max(PreviewSize, (int)Math.Round(sourceHeight * scale));
        var resized = Resize(source, scaledWidth, scaledHeight);
        var crop = new CroppedBitmap(resized, GetFocusedCrop(scaledWidth, scaledHeight, PreviewSize, PreviewSize, options));
        crop.Freeze();
        return crop;
    }

    private static BitmapSource FitWithPaddingPreview(BitmapSource source, int sourceWidth, int sourceHeight, ImageTransformOptions options)
    {
        var scale = Math.Min((double)PreviewSize / sourceWidth, (double)PreviewSize / sourceHeight) *
                    Math.Clamp(options.Zoom, 1.0, 4.0);
        var scaledWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
        var scaledHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));
        var resized = Resize(source, scaledWidth, scaledHeight);
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            if (!options.UseTransparentPadding)
                context.DrawRectangle(Brushes.White, null, new System.Windows.Rect(0, 0, PreviewSize, PreviewSize));

            var maxOffsetX = Math.Max(0, scaledWidth - PreviewSize);
            var maxOffsetY = Math.Max(0, scaledHeight - PreviewSize);
            var left = scaledWidth <= PreviewSize
                ? (PreviewSize - scaledWidth) / 2.0
                : -maxOffsetX * Math.Clamp(options.FocusX, 0.0, 1.0);
            var top = scaledHeight <= PreviewSize
                ? (PreviewSize - scaledHeight) / 2.0
                : -maxOffsetY * Math.Clamp(options.FocusY, 0.0, 1.0);

            context.DrawImage(resized, new System.Windows.Rect(left, top, scaledWidth, scaledHeight));
        }

        var bitmap = new RenderTargetBitmap(PreviewSize, PreviewSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource Resize(BitmapSource source, int width, int height)
    {
        var scaleX = (double)width / Math.Max(1, source.PixelWidth);
        var scaleY = (double)height / Math.Max(1, source.PixelHeight);
        var transformed = new TransformedBitmap(source, new ScaleTransform(scaleX, scaleY, 0, 0));
        transformed.Freeze();
        return transformed;
    }

    private static System.Windows.Int32Rect GetFocusedCrop(int sourceWidth, int sourceHeight, int cropWidth, int cropHeight, ImageTransformOptions options)
    {
        cropWidth = Math.Clamp(cropWidth, 1, sourceWidth);
        cropHeight = Math.Clamp(cropHeight, 1, sourceHeight);
        var x = (int)Math.Round((sourceWidth - cropWidth) * Math.Clamp(options.FocusX, 0.0, 1.0));
        var y = (int)Math.Round((sourceHeight - cropHeight) * Math.Clamp(options.FocusY, 0.0, 1.0));

        return new System.Windows.Int32Rect(
            Math.Clamp(x, 0, Math.Max(0, sourceWidth - cropWidth)),
            Math.Clamp(y, 0, Math.Max(0, sourceHeight - cropHeight)),
            cropWidth,
            cropHeight);
    }
}
