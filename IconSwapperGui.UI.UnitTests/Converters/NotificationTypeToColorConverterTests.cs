using System.Globalization;
using System.Windows.Media;
using IconSwapperGui.Core.Models;
using IconSwapperGui.UI.Converters;

namespace IconSwapperGui.UI.UnitTests.Converters;

[TestFixture]
public class NotificationTypeToColorConverterTests
{
    [TestCase(NotificationType.Info, "#3B82F6")]
    [TestCase(NotificationType.Success, "#10B981")]
    [TestCase(NotificationType.Warning, "#F59E0B")]
    [TestCase(NotificationType.Error, "#EF4444")]
    public void Convert_ReturnsExpectedBrush(NotificationType type, string expectedHex)
    {
        var conv = new NotificationTypeToColorConverter();

        var result = conv.Convert(type, typeof(Brush), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.TypeOf<SolidColorBrush>());
        var brush = (SolidColorBrush)result;
        var hex = brush.Color.ToString();
        Assert.That(hex, Is.EqualTo(ColorConverter.ConvertFromString(expectedHex)!.ToString()));
    }

    [Test]
    public void Convert_Invalid_ReturnsDefaultBrush()
    {
        var conv = new NotificationTypeToColorConverter();

        var result = conv.Convert(null, typeof(Brush), null, CultureInfo.InvariantCulture);

        Assert.That(result, Is.TypeOf<SolidColorBrush>());
    }
}