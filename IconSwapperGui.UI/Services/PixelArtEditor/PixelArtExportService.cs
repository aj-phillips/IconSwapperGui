using IconSwapperGui.Core.Interfaces;
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IconSwapperGui.UI.Services.PixelArtEditor;

public sealed class PixelArtExportService
{
    private readonly IIconCreatorService _iconCreatorService;

    public PixelArtExportService(IIconCreatorService iconCreatorService)
    {
        _iconCreatorService = iconCreatorService;
    }

    public void ExportPngAndIco(string folderPath, string baseFileName, int rows, int columns, ReadOnlySpan<uint> argbPixels, Color background)
    {
        if (string.IsNullOrWhiteSpace(folderPath)) throw new InvalidOperationException("Export folder is not configured.");
        if (string.IsNullOrWhiteSpace(baseFileName)) throw new InvalidOperationException("File name is required.");
        if (rows <= 0) throw new InvalidOperationException("Rows must be > 0.");
        if (columns <= 0) throw new InvalidOperationException("Columns must be > 0.");
        if (argbPixels.Length != checked(rows * columns)) throw new InvalidOperationException("Pixel buffer size does not match layout.");

        Directory.CreateDirectory(folderPath);

        var safeBase = GetSafeBaseNameFor(baseFileName);
        var pngPath = Path.Combine(folderPath, $"{safeBase}.png");
        var icoPath = Path.Combine(folderPath, $"{safeBase}.ico");

        SavePng(pngPath, rows, columns, argbPixels, background);

        _iconCreatorService.CreateIcoFromPng(pngPath, icoPath);
    }

    private static void SavePng(string pngPath, int rows, int columns, ReadOnlySpan<uint> argbPixels, Color background)
    {
        // WPF WriteableBitmap expects BGRA32.
        var pixels = new byte[checked(rows * columns * 4)];

        for (var i = 0; i < argbPixels.Length; i++)
        {
            var argb = argbPixels[i];
            var a = (byte)((argb >> 24) & 0xFF);
            var r = (byte)((argb >> 16) & 0xFF);
            var g = (byte)((argb >> 8) & 0xFF);
            var b = (byte)(argb & 0xFF);

            var offset = i * 4;
            pixels[offset + 0] = b;
            pixels[offset + 1] = g;
            pixels[offset + 2] = r;
            pixels[offset + 3] = a;
        }

        var wb = new WriteableBitmap(columns, rows, 96, 96, PixelFormats.Bgra32, null);
        wb.WritePixels(new System.Windows.Int32Rect(0, 0, columns, rows), pixels, columns * 4, 0);

        using var stream = new FileStream(pngPath, FileMode.Create, FileAccess.Write, FileShare.None);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(wb));
        encoder.Save(stream);
    }

    private static string GetSafeBaseNameFor(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed)) trimmed = "Pixel_Art";

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            trimmed = trimmed.Replace(c, '_');
        }

        while (trimmed.Contains("__", StringComparison.Ordinal)) trimmed = trimmed.Replace("__", "_", StringComparison.Ordinal);

        return trimmed;
    }
}
