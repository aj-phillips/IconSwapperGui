using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.UI.Services.Swapper;
using Moq;

namespace IconSwapperGui.UI.UnitTests.Services;

[TestFixture]
public sealed class IconManagementServiceTests
{
    private string _tempDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "icon-management", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public async Task DeleteIconAsync_WhenIconMissing_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.DeleteIconAsync(Path.Combine(_tempDir, "missing.ico"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Title, Is.EqualTo("Icon Not Found"));
        });
    }

    [Test]
    public async Task DeleteIconAsync_WhenIconExists_DeletesFile()
    {
        var service = CreateService();
        var iconPath = Path.Combine(_tempDir, "icon.ico");
        File.WriteAllText(iconPath, "ico");

        var result = await service.DeleteIconAsync(iconPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(File.Exists(iconPath), Is.False);
        });
    }

    [Test]
    public async Task RenameIconAsync_WhenTargetExists_ReturnsFailure()
    {
        var service = CreateService();
        var iconPath = Path.Combine(_tempDir, "icon.ico");
        var existingPath = Path.Combine(_tempDir, "existing.ico");
        File.WriteAllText(iconPath, "ico");
        File.WriteAllText(existingPath, "ico");

        var result = await service.RenameIconAsync(iconPath, "existing");

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Title, Is.EqualTo("Rename Failed"));
            Assert.That(File.Exists(iconPath), Is.True);
        });
    }

    private static IconManagementService CreateService()
        => new(new Mock<ILoggingService>().Object);
}
