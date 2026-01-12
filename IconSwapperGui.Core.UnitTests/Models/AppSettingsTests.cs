using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Core.UnitTests.Models;

[TestFixture]
public class AppSettingsTests
{
    [Test]
    public void Defaults_ContainAllSubsettings()
    {
        var settings = new AppSettings();

        Assert.Multiple(() =>
        {
            Assert.That(settings.Appearance, Is.Not.Null);
            Assert.That(settings.General, Is.Not.Null);
            Assert.That(settings.Notifications, Is.Not.Null);
            Assert.That(settings.Advanced, Is.Not.Null);
        });
    }
}