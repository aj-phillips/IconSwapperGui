using IconSwapperGui.UI.Services.Swapper;

namespace IconSwapperGui.UI.UnitTests.Services;

[TestFixture]
public sealed class UrlSwapperServiceTests
{
    private string _tempDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "url-swapper", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public void Swap_WhenShortcutMissing_ReturnsFailure()
    {
        var service = new UrlSwapperService();
        var iconPath = Path.Combine(_tempDir, "icon.ico");
        File.WriteAllText(iconPath, "ico");

        var result = service.Swap(Path.Combine(_tempDir, "missing.url"), iconPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Title, Is.EqualTo("Shortcut Not Found"));
        });
    }

    [Test]
    public void Swap_WhenIconIsNotIco_ReturnsFailure()
    {
        var service = new UrlSwapperService();
        var shortcutPath = Path.Combine(_tempDir, "game.url");
        var iconPath = Path.Combine(_tempDir, "icon.png");
        File.WriteAllText(shortcutPath, "[InternetShortcut]");
        File.WriteAllText(iconPath, "png");

        var result = service.Swap(shortcutPath, iconPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Title, Is.EqualTo("Unsupported Icon"));
        });
    }

    [Test]
    public void Swap_WhenIconFileLineExists_ReplacesIconFile()
    {
        var service = new UrlSwapperService();
        var shortcutPath = Path.Combine(_tempDir, "game.url");
        var iconPath = Path.Combine(_tempDir, "icon.ico");
        File.WriteAllLines(shortcutPath, ["[InternetShortcut]", "URL=steam://rungameid/1", "IconFile=old.ico"]);
        File.WriteAllText(iconPath, "ico");

        var result = service.Swap(shortcutPath, iconPath);
        var updated = File.ReadAllLines(shortcutPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(updated, Does.Contain("IconFile=" + iconPath));
            Assert.That(updated, Does.Not.Contain("IconFile=old.ico"));
        });
    }
}
