using System.Globalization;
using System.Windows;
using IconSwapperGui.UI.Converters;

namespace IconSwapperGui.UI.UnitTests.Converters;

[TestFixture]
public class SectionVisibilityConverterTests
{
    [Test]
    public void Convert_MatchingSection_ReturnsVisible()
    {
        var conv = new SectionVisibilityConverter();
        var r = conv.Convert("Appearance", typeof(Visibility), "Appearance", CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo(Visibility.Visible));
    }

    [Test]
    public void Convert_NonMatchingSection_ReturnsCollapsed()
    {
        var conv = new SectionVisibilityConverter();
        var r = conv.Convert("General", typeof(Visibility), "Appearance", CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo(Visibility.Collapsed));
    }

    [Test]
    public void Convert_NullValues_ReturnsCollapsed()
    {
        var conv = new SectionVisibilityConverter();
        var r = conv.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo(Visibility.Collapsed));
    }
}