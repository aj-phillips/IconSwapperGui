using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Swapper.IconVersionManagement;
using IconSwapperGui.UI.ViewModels;
using Moq;

namespace IconSwapperGui.UI.UnitTests.ViewModels;

[TestFixture]
public sealed class IconVersionManagerViewModelTests
{
    [Test]
    public async Task RevertToSelectedVersionAsync_WhenRevertSucceeds_RefreshesShellIcon()
    {
        var filePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "shortcut.lnk");
        var version = new IconVersion
        {
            Id = Guid.NewGuid(),
            IconPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "icon.ico"),
            ThumbnailPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "thumb.png"),
            Timestamp = DateTime.Now.AddMinutes(-5),
            IsCurrent = false
        };
        var history = new IconHistory { FilePath = filePath };
        history.Versions.Add(version);

        var historyService = new Mock<IIconHistoryService>();
        historyService.Setup(service => service.GetHistoryAsync(filePath)).ReturnsAsync(history);
        historyService.Setup(service => service.RevertToVersionAsync(filePath, version.Id)).ReturnsAsync(true);
        var dialogService = new Mock<IDialogService>();
        dialogService.Setup(service => service.Confirm(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        var shellRefreshService = new Mock<IShellIconRefreshService>();

        var viewModel = new IconVersionManagerViewModel(
            historyService.Object,
            new Mock<ILoggingService>().Object,
            new Mock<INotificationService>().Object,
            dialogService.Object,
            shellRefreshService.Object,
            filePath)
        {
            SelectedVersion = version
        };

        await viewModel.RevertToSelectedVersionCommand.ExecuteAsync(null);

        shellRefreshService.Verify(service => service.RefreshIcon(filePath), Times.Once);
    }
}
