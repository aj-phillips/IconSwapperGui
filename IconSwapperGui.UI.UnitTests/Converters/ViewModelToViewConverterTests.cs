using System.Globalization;
using Moq;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.UI.Converters;
using IconSwapperGui.UI.ViewModels;

namespace IconSwapperGui.UI.UnitTests.Converters;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class ViewModelToViewConverterTests
{
    [Test]
    public void Convert_SwapperViewModel_ReturnsView()
    {
        var conv = new ViewModelToViewConverter();
        var settingsMock = new Mock<ISettingsService>();
        var notifMock = new Mock<INotificationService>();
        var shortcutServiceMock = new Mock<IShortcutService>();
        var iconManagementServiceMock = new Mock<IIconManagementService>();
        var iconHistoryServiceMock = new Mock<IIconHistoryService>();
        var folderManagementServiceMock = new Mock<IFolderManagementService>();
        var loggingServiceMock = new Mock<ILoggingService>();
        var lnkSwapperServiceMock = new Mock<ILnkSwapperService>();
        var urlSwapperServiceMock = new Mock<IUrlSwapperService>();
        Func<string, IconVersionManagerViewModel> factory = _ =>
            new Mock<IconVersionManagerViewModel>(
                    new Mock<IIconHistoryService>().Object,
                    new Mock<ILoggingService>().Object,
                    new Mock<INotificationService>().Object,
                    _)
                .Object;
        var vm = new SwapperViewModel(settingsMock.Object, notifMock.Object, shortcutServiceMock.Object,
            iconManagementServiceMock.Object, iconHistoryServiceMock.Object, folderManagementServiceMock.Object,
            loggingServiceMock.Object, lnkSwapperServiceMock.Object, urlSwapperServiceMock.Object, factory);
        var view = conv.Convert(vm, typeof(object), null, CultureInfo.InvariantCulture);
        Assert.That(view, Is.Not.Null);
    }

    [Test]
    public void Convert_SettingsViewModel_ReturnsView()
    {
        var conv = new ViewModelToViewConverter();

        var settings = new AppSettings();
        var settingsMock = new Mock<ISettingsService>();
        settingsMock.SetupGet(s => s.Settings).Returns(settings);
        settingsMock.Setup(s => s.SaveSettingsAsync()).Returns(Task.CompletedTask);

        var themeMock = new Mock<IThemeService>();
        var notifMock = new Mock<INotificationService>();
        var updateMock = new Mock<IUpdateService>();

        var vm = new SettingsViewModel(settingsMock.Object, themeMock.Object, notifMock.Object, updateMock.Object);

        var view = conv.Convert(vm, typeof(object), null, CultureInfo.InvariantCulture);
        Assert.That(view, Is.Not.Null);
    }
}