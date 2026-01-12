using System.Globalization;
using System.Windows;
using IconSwapperGui.UI.Converters;

namespace IconSwapperGui.UI.UnitTests.Converters;

[TestFixture]
public class BoolToVisibilityConverterTests
{
    [Test]
    public void Convert_BoolTrue_ReturnsVisible()
    {
        var conv = new BoolToVisibilityConverter();
        var r = conv.Convert(true, typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo(Visibility.Visible));
    }

    [Test]
    public void Convert_BoolFalse_ReturnsCollapsed()
    {
        var conv = new BoolToVisibilityConverter();
        var r = conv.Convert(false, typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo(Visibility.Collapsed));
    }

    [Test]
    public void Convert_IntGreaterThanZero_ReturnsVisible()
    {
        var conv = new BoolToVisibilityConverter();
        var r = conv.Convert(5, typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo(Visibility.Visible));
    }

    [Test]
    public void Convert_InvertParameter_InvertsResult()
    {
        var conv = new BoolToVisibilityConverter();
        var r = conv.Convert(true, typeof(Visibility), "Invert", CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo(Visibility.Collapsed));
    }
}