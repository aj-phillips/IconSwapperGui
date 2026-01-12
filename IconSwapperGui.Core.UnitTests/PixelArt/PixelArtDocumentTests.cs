using IconSwapperGui.Core.PixelArt;
using NUnit.Framework;

namespace IconSwapperGui.Core.UnitTests.PixelArt;

[TestFixture]
public sealed class PixelArtDocumentTests
{
    [Test]
    public void WhenClearThenAllPixelsBecomeBackground()
    {
        var doc = new PixelArtDocument(2, 2);
        doc.Clear(0xFF112233);

        Assert.That(doc.PixelsArgb[0], Is.EqualTo(0xFF112233));
    }

    [Test]
    public void WhenSetCellThenPixelIsUpdated()
    {
        var doc = new PixelArtDocument(2, 2);
        doc.Clear(0xFFFFFFFF);
        doc.SetCell(3, 0xFF000000);

        Assert.That(doc.GetCell(3), Is.EqualTo(0xFF000000));
    }

    [Test]
    public void WhenFloodFillThenConnectedRegionIsReplaced()
    {
        // 2x2
        // [0][1]
        // [2][3]
        var doc = new PixelArtDocument(2, 2);
        doc.Clear(0xFFFFFFFF);
        doc.SetCell(3, 0xFF000000); // different

        doc.FloodFill(0, 0xFF00FF00);

        Assert.That(doc.GetCell(0), Is.EqualTo(0xFF00FF00));
    }
}
