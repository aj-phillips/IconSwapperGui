using System.Windows.Media;
using System.Windows.Media.Imaging;
using IconSwapperGui.UI.Converters;

namespace IconSwapperGui.UI.UnitTests.Converters;

[TestFixture]
[Apartment(ApartmentState.STA)]
public sealed class ImagePathToImageConverterTests
{
    private string _tempDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "image-preview", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public void Convert_WhenImageExists_LoadsActualBitmap()
    {
        var path = Path.Combine(_tempDir, "preview.png");
        SavePng(path, Colors.CornflowerBlue);
        var converter = new ImagePathToImageConverter();

        var result = converter.Convert(path, typeof(BitmapImage), null, System.Globalization.CultureInfo.InvariantCulture);

        Assert.That(result, Is.TypeOf<BitmapImage>());
        var image = (BitmapImage)result!;
        Assert.Multiple(() =>
        {
            Assert.That(image.PixelWidth, Is.EqualTo(8));
            Assert.That(image.PixelHeight, Is.EqualTo(8));
        });
    }

    [Test]
    public void Convert_WhenImageIsMissing_ReturnsNull()
    {
        var converter = new ImagePathToImageConverter();

        var result = converter.Convert(Path.Combine(_tempDir, "missing.png"), typeof(BitmapImage), null, System.Globalization.CultureInfo.InvariantCulture);

        Assert.That(result, Is.Null);
    }

    private static void SavePng(string path, Color color)
    {
        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.DrawRectangle(new SolidColorBrush(color), null, new System.Windows.Rect(0, 0, 8, 8));
        }

        var bitmap = new RenderTargetBitmap(8, 8, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        using var stream = File.Create(path);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(stream);
    }
}
