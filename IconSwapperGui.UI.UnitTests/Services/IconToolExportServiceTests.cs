using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.UI.Services.IconTools;
using Moq;
using System.Windows.Media;

namespace IconSwapperGui.UI.UnitTests.Services;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class IconToolExportServiceTests
{
    [Test]
    public void RenderFolderThemeBitmap_ReturnsVisiblePreviewPixels()
    {
        var service = new IconToolExportService(new Mock<IIconCreatorService>().Object);

        var bitmap = service.RenderFolderThemeBitmap(
            256,
            Color.FromRgb(37, 99, 235),
            Color.FromRgb(79, 70, 229),
            "W",
            "Work");

        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

        Assert.That(pixels.Where((_, index) => index % 4 == 3).Any(alpha => alpha > 0), Is.True);
    }

    [Test]
    public void RenderFolderThemeBitmap_WithCreativeOptions_ReturnsVisiblePreviewPixels()
    {
        var service = new IconToolExportService(new Mock<IIconCreatorService>().Object);

        var bitmap = service.RenderFolderThemeBitmap(
            256,
            new FolderThemeRenderOptions
            {
                FolderColor = Color.FromRgb(255, 190, 45),
                AccentColor = Color.FromRgb(79, 70, 229),
                Badge = "P",
                Label = "Project",
                ShowBadge = true,
                ShowDocument = true,
                StyleName = "Project",
                TabStyleName = "Angled",
                LabelPlacementName = "Front",
                LabelFontSize = 28,
                Depth = 0.85,
                CornerSoftness = 12
            });

        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

        Assert.That(pixels.Where((_, index) => index % 4 == 3).Any(alpha => alpha > 0), Is.True);
    }

    [Test]
    public void RenderFolderThemeBitmap_DrawsConnectedTabAndBackPanel()
    {
        var service = new IconToolExportService(new Mock<IIconCreatorService>().Object);

        var bitmap = service.RenderFolderThemeBitmap(
            256,
            new FolderThemeRenderOptions
            {
                FolderColor = Color.FromRgb(255, 190, 45),
                AccentColor = Color.FromRgb(79, 70, 229),
                Badge = "W",
                Label = "Work",
                ShowBadge = true,
                ShowDocument = true,
                StyleName = "Windows",
                TabStyleName = "Angled",
                LabelPlacementName = "Front",
                LabelFontSize = 26,
                Depth = 0.7,
                CornerSoftness = 12
            });

        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);

        Assert.Multiple(() =>
        {
            Assert.That(GetAlpha(pixels, bitmap.PixelWidth, 120, 76), Is.GreaterThan(0));
            Assert.That(GetAlpha(pixels, bitmap.PixelWidth, 120, 82), Is.GreaterThan(0));
        });
    }

    private static byte GetAlpha(byte[] pixels, int width, int x, int y)
        => pixels[((y * width) + x) * 4 + 3];
}
