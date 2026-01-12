using Moq;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.UI.ViewModels;

namespace IconSwapperGui.UI.UnitTests.ViewModels;

[TestFixture]
public class MainWindowViewModelTests
{
    [Test]
    public void Ctor_NavigatesToSwapperAndWiresEvents()
    {
        var navMock = new Mock<INavigationService>();
        var notificationPanel =
            new Mock<NotificationPanelViewModel>(MockBehavior.Loose, new Mock<INotificationService>().Object).Object;

        navMock.Setup(n => n.NavigateTo<SwapperViewModel>());

        var vm = new MainWindowViewModel(navMock.Object, notificationPanel);

        navMock.Verify(n => n.NavigateTo<SwapperViewModel>(), Times.Once);
        Assert.That(vm.PageTitle, Is.EqualTo("Swapper"));
    }

    [Test]
    public void NavigateCommands_ChangePageTitle()
    {
        var navMock = new Mock<INavigationService>();
        var notificationPanel =
            new Mock<NotificationPanelViewModel>(MockBehavior.Loose, new Mock<INotificationService>().Object).Object;
        var vm = new MainWindowViewModel(navMock.Object, notificationPanel);

        vm.NavigateToSettingsCommand.Execute(null);
        Assert.That(vm.PageTitle, Is.EqualTo("Settings"));

        vm.NavigateToSwapperCommand.Execute(null);
        Assert.That(vm.PageTitle, Is.EqualTo("Swapper"));
    }
}