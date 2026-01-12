using System.Globalization;
using IconSwapperGui.UI.Converters;

namespace IconSwapperGui.UI.UnitTests.Converters;

[TestFixture]
public class TimeAgoConverterTests
{
    [Test]
    public void Convert_JustNow()
    {
        var conv = new TimeAgoConverter();
        var r = conv.Convert(DateTime.UtcNow, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo("Just now"));
    }

    [Test]
    public void Convert_MinutesAgo()
    {
        var conv = new TimeAgoConverter();
        var r = conv.Convert(DateTime.UtcNow.AddMinutes(-5), typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo("5m ago"));
    }

    [Test]
    public void Convert_HoursAgo()
    {
        var conv = new TimeAgoConverter();
        var r = conv.Convert(DateTime.UtcNow.AddHours(-3), typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo("3h ago"));
    }

    [Test]
    public void Convert_DaysAgo()
    {
        var conv = new TimeAgoConverter();
        var r = conv.Convert(DateTime.UtcNow.AddDays(-2), typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(r, Is.EqualTo("2d ago"));
    }
}