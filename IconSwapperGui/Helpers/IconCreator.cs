using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IconSwapperGui.Helpers;

public static class IconCreator
{
    public static void CreateIcoFromPng(string pngPath, string icoPath, int targetSize = 128)
    {
        if (string.IsNullOrWhiteSpace(pngPath)) throw new ArgumentNullException(nameof(pngPath));
        if (string.IsNullOrWhiteSpace(icoPath)) throw new ArgumentNullException(nameof(icoPath));
        if (!File.Exists(pngPath)) throw new FileNotFoundException("PNG source not found", pngPath);

        CreateMultiSizeIcoFromImage(pngPath, icoPath, new[] { targetSize });
    }

    public static void CreateMultiSizeIcoFromImage(string sourceImagePath, string icoPath, int[] sizes)
    {
        if (string.IsNullOrWhiteSpace(sourceImagePath)) throw new ArgumentNullException(nameof(sourceImagePath));
        if (string.IsNullOrWhiteSpace(icoPath)) throw new ArgumentNullException(nameof(icoPath));
        if (sizes == null || sizes.Length == 0) throw new ArgumentException("Sizes must be provided", nameof(sizes));
        if (!File.Exists(sourceImagePath)) throw new FileNotFoundException("Source image not found", sourceImagePath);

        var distinctSizes = sizes.Distinct().OrderBy(s => s).ToArray();

        var decoder = BitmapDecoder.Create(new Uri(sourceImagePath), BitmapCreateOptions.PreservePixelFormat,
            BitmapCacheOption.OnLoad);

        var frame = decoder.Frames[0];

        var pngEntries = new (int Size, byte[] Data)[distinctSizes.Length];

        for (var i = 0; i < distinctSizes.Length; i++)
        {
            var target = distinctSizes[i];

            BitmapSource toEncode;

            if (frame.PixelWidth == target && frame.PixelHeight == target)
            {
                toEncode = frame;
            }
            else
            {
                var scaleX = (double)target / Math.Max(1, frame.PixelWidth);
                var scaleY = (double)target / Math.Max(1, frame.PixelHeight);
                var transform = new ScaleTransform(scaleX, scaleY, 0, 0);
                toEncode = new TransformedBitmap(frame, transform);
            }

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
}