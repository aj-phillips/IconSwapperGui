using IconSwapperGui.Core.Models.Settings;

namespace IconSwapperGui.Core.UnitTests.Models;

[TestFixture]
public class NotificationSettingsTests
{
    [Test]
    public void Defaults_AreExpected()
    {
        var settings = new NotificationSettings();

        Assert.Multiple(() => { Assert.That(settings.PlaySound, Is.False); });
    }
}