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
}
