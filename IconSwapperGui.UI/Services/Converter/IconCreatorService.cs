using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Images;

namespace IconSwapperGui.UI.Services.Converter;

public class IconCreatorService : IIconCreatorService
{
    public void CreateIcoFromPng(string pngPath, string icoPath, int targetSize = 128)
    {
        if (string.IsNullOrWhiteSpace(pngPath)) throw new ArgumentNullException(nameof(pngPath));
        if (string.IsNullOrWhiteSpace(icoPath)) throw new ArgumentNullException(nameof(icoPath));
        if (!File.Exists(pngPath)) throw new FileNotFoundException("PNG source not found", pngPath);

        CreateMultiSizeIcoFromImage(pngPath, icoPath, new[] { targetSize });
    }

    public void CreateMultiSizeIcoFromImage(string sourceImagePath, string icoPath, int[] sizes, ImageTransformOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(sourceImagePath)) throw new ArgumentNullException(nameof(sourceImagePath));
        if (string.IsNullOrWhiteSpace(icoPath)) throw new ArgumentNullException(nameof(icoPath));
        if (sizes == null || sizes.Length == 0) throw new ArgumentException("Sizes must be provided", nameof(sizes));
        if (!File.Exists(sourceImagePath)) throw new FileNotFoundException("Source image not found", sourceImagePath);

        options ??= new ImageTransformOptions();
        var distinctSizes = sizes.Distinct().OrderBy(s => s).ToArray();

        var decoder = BitmapDecoder.Create(new Uri(sourceImagePath), BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        var pngEntries = new (int Size, byte[] Data)[distinctSizes.Length];

        for (var i = 0; i < distinctSizes.Length; i++)
        {
            var target = distinctSizes[i];
            var toEncode = PrepareSquareFrame(frame, target, options);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(toEncode));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            pngEntries[i] = (target, ms.ToArray());
        }

        using var fs = File.Create(icoPath);
        using var bw = new BinaryWriter(fs);

        bw.Write((short)0);
        bw.Write((short)1);
        bw.Write((short)pngEntries.Length);

        var entryCount = pngEntries.Length;
        var imageDataOffset = 6 + (16 * entryCount);

        for (var i = 0; i < entryCount; i++)
        {
            var size = pngEntries[i].Size;
            var data = pngEntries[i].Data;
            var bsize = (byte)(size >= 256 ? 0 : size);

            bw.Write(bsize);
            bw.Write(bsize);
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(data.Length);
            bw.Write(imageDataOffset);

            imageDataOffset += data.Length;
        }

        for (var i = 0; i < entryCount; i++)
        {
            bw.Write(pngEntries[i].Data);
        }
    }

    private static BitmapSource PrepareSquareFrame(BitmapSource source, int target, ImageTransformOptions options)
    {
        var sourceWidth = Math.Max(1, source.PixelWidth);
        var sourceHeight = Math.Max(1, source.PixelHeight);

        return options.CropMode switch
        {
            ImageCropMode.FitWithPadding => FitWithPadding(source, target, sourceWidth, sourceHeight, options.UseTransparentPadding),
            ImageCropMode.FillAndCrop => FillAndCrop(source, target, sourceWidth, sourceHeight, options),
            _ => CenterCrop(source, target, sourceWidth, sourceHeight, options)
        };
    }

    private static BitmapSource CenterCrop(BitmapSource source, int target, int sourceWidth, int sourceHeight, ImageTransformOptions options)
    {
        var zoom = Math.Clamp(options.Zoom, 1.0, 4.0);
        var side = Math.Max(1, (int)Math.Round(Math.Min(sourceWidth, sourceHeight) / zoom));
        var crop = new CroppedBitmap(source, GetFocusedCrop(sourceWidth, sourceHeight, side, side, options));

        return Resize(crop, target, target);
    }

    private static BitmapSource FillAndCrop(BitmapSource source, int target, int sourceWidth, int sourceHeight, ImageTransformOptions options)
    {
        var scale = Math.Max((double)target / sourceWidth, (double)target / sourceHeight) * Math.Clamp(options.Zoom, 1.0, 4.0);
        var scaledWidth = Math.Max(target, (int)Math.Round(sourceWidth * scale));
        var scaledHeight = Math.Max(target, (int)Math.Round(sourceHeight * scale));
        var resized = Resize(source, scaledWidth, scaledHeight);

        return new CroppedBitmap(resized, GetFocusedCrop(scaledWidth, scaledHeight, target, target, options));
    }

    private static BitmapSource FitWithPadding(BitmapSource source, int target, int sourceWidth, int sourceHeight, bool transparentPadding)
    {
        var scale = Math.Min((double)target / sourceWidth, (double)target / sourceHeight);
        var scaledWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
        var scaledHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));
        var resized = Resize(source, scaledWidth, scaledHeight);

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            if (!transparentPadding)
                context.DrawRectangle(Brushes.White, null, new System.Windows.Rect(0, 0, target, target));

            context.DrawImage(resized, new System.Windows.Rect(
                (target - scaledWidth) / 2.0,
                (target - scaledHeight) / 2.0,
                scaledWidth,
                scaledHeight));
        }

        var bitmap = new RenderTargetBitmap(target, target, 96, 96, PixelFormats.Pbgra32);
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
        var focusX = Math.Clamp(options.FocusX, 0.0, 1.0);
        var focusY = Math.Clamp(options.FocusY, 0.0, 1.0);
        var x = (int)Math.Round((sourceWidth - cropWidth) * focusX);
        var y = (int)Math.Round((sourceHeight - cropHeight) * focusY);

        return new System.Windows.Int32Rect(
            Math.Clamp(x, 0, Math.Max(0, sourceWidth - cropWidth)),
            Math.Clamp(y, 0, Math.Max(0, sourceHeight - cropHeight)),
            cropWidth,
            cropHeight);
    }
}
