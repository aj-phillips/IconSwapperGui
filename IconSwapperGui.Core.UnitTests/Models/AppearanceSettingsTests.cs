using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Settings;

namespace IconSwapperGui.Core.UnitTests.Models;

[TestFixture]
public class AppearanceSettingsTests
{
    [Test]
    public void Defaults_AreExpected()
    {
        var settings = new AppearanceSettings();

        Assert.Multiple(() =>
        {
            Assert.That(settings.Theme, Is.EqualTo(ThemeMode.Light));
            Assert.That(settings.UseSystemTheme, Is.False);
        });
    }
}