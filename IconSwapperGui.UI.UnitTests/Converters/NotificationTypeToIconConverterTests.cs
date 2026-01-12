using System.Globalization;
using IconSwapperGui.Core.Models;
using IconSwapperGui.UI.Converters;

namespace IconSwapperGui.UI.UnitTests.Converters;

[TestFixture]
public class NotificationTypeToIconConverterTests
{
    [TestCase(NotificationType.Info)]
    [TestCase(NotificationType.Success)]
    [TestCase(NotificationType.Warning)]
    [TestCase(NotificationType.Error)]
    public void Convert_ReturnsString(NotificationType type)
    {
        var conv = new NotificationTypeToIconConverter();
        var result = conv.Convert(type, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(result, Is.TypeOf<string>());
        Assert.That(((string)result).Length, Is.GreaterThan(0));
    }

    [Test]
    public void Convert_Invalid_ReturnsDefault()
    {
        var conv = new NotificationTypeToIconConverter();
        var result = conv.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.That(result, Is.TypeOf<string>());
    }
}