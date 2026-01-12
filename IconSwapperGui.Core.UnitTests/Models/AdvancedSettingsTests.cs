using IconSwapperGui.Core.Models.Settings;

namespace IconSwapperGui.Core.UnitTests.Models;

[TestFixture]
public class AdvancedSettingsTests
{
    [Test]
    public void Defaults_AreExpected()
    {
        var settings = new AdvancedSettings();

        Assert.Multiple(() => { Assert.That(settings.EnableLogging, Is.True); });
    }
}