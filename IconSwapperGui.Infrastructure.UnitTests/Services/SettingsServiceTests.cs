using IconSwapperGui.Infrastructure.Services;

namespace IconSwapperGui.Infrastructure.UnitTests.Services;

[TestFixture]
public class SettingsServiceTests
{
    [Test]
    public void Ctor_CreatesPaths()
    {
        var svc = new SettingsService();

        var appData = svc.GetAppDataPath();
        var settingsPath = svc.GetSettingsFilePath();

        Assert.Multiple(() =>
        {
            Assert.That(Directory.Exists(appData), Is.True);
            Assert.That(Path.GetDirectoryName(settingsPath), Is.EqualTo(appData));
        });
    }

    [Test]
    public async Task SaveAndLoadSettingsAsync_WritesAndReadsFile()
    {
        var svc = new SettingsService();
        svc.Settings.General.CheckForUpdates = true;

        await svc.SaveSettingsAsync();

        var path = svc.GetSettingsFilePath();

        Assert.That(File.Exists(path), Is.True);

        svc.Settings.General.CheckForUpdates = false;

        await svc.LoadSettingsAsync();

        Assert.That(svc.Settings.General.CheckForUpdates, Is.True);
    }

    [Test]
    public async Task LoadSettingsAsync_WhenJsonIsInvalid_DoesNotOverwriteExistingFile()
    {
        var svc = new SettingsService();
        svc.Settings.General.CheckForUpdates = true;
        await svc.SaveSettingsAsync();

        var path = svc.GetSettingsFilePath();
        var before = await File.ReadAllTextAsync(path);

        await File.WriteAllTextAsync(path, "{ invalid json");

        await svc.LoadSettingsAsync();

        var after = await File.ReadAllTextAsync(path);
        Assert.That(after, Is.EqualTo("{ invalid json"));
    }
}