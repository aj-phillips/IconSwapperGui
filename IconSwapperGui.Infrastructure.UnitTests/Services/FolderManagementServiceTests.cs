using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Infrastructure.Services;
using Moq;

namespace IconSwapperGui.Infrastructure.UnitTests.Services;

[TestFixture]
public sealed class FolderManagementServiceTests
{
    private string _tempDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "folder-management", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public async Task ChangeFolderIconAsync_WhenFolderMissing_ReturnsFailure()
    {
        var service = CreateService();
        var iconPath = Path.Combine(_tempDir, "icon.ico");
        File.WriteAllText(iconPath, "ico");

        var result = await service.ChangeFolderIconAsync(Path.Combine(_tempDir, "missing"), iconPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Title, Is.EqualTo("Folder Not Found"));
        });
    }

    [Test]
    public async Task ChangeFolderIconAsync_WhenIconIsNotIco_ReturnsFailure()
    {
        var service = CreateService();
        var folderPath = Path.Combine(_tempDir, "folder");
        var iconPath = Path.Combine(_tempDir, "icon.png");
        Directory.CreateDirectory(folderPath);
        File.WriteAllText(iconPath, "png");

        var result = await service.ChangeFolderIconAsync(folderPath, iconPath);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Title, Is.EqualTo("Unsupported Icon"));
        });
    }

    private static FolderManagementService CreateService()
        => new(new Mock<ILoggingService>().Object);
}
