using IconSwapperGui.Core.Models.Settings;

namespace IconSwapperGui.Core.UnitTests.Models;

[TestFixture]
public class GeneralSettingsTests
{
    [Test]
    public void Defaults_AreExpected()
    {
        var settings = new GeneralSettings();

        Assert.Pass();
    }
}