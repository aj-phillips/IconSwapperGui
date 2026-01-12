using Moq;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Settings;
using IconSwapperGui.UI.ViewModels;

namespace IconSwapperGui.UI.UnitTests.ViewModels;

[TestFixture]
public class SettingsViewModelTests
{
    [Test]
    public void Ctor_InitializesPropertiesFromSettings()
    {
        var settings = new AppSettings();
        settings.Appearance.Theme = ThemeMode.Dark;
        settings.General.CheckForUpdates = true;

        var settingsMock = new Mock<ISettingsService>();
        settingsMock.SetupGet(s => s.Settings).Returns(settings);
        settingsMock.Setup(s => s.SaveSettingsAsync()).Returns(Task.CompletedTask);

        var themeMock = new Mock<IThemeService>();
        var notifMock = new Mock<INotificationService>();
        var updateMock = new Mock<IUpdateService>();

        var vm = new SettingsViewModel(settingsMock.Object, themeMock.Object, notifMock.Object, updateMock.Object);

        Assert.Multiple(() => { Assert.That(vm.CheckForUpdates, Is.True); });
    }

    [Test]
    public void ResetToDefaultsAsync_ResetsAndNotifies()
    {
        var settings = new AppSettings();
        settings.Appearance.Theme = ThemeMode.Dark;

        var settingsMock = new Mock<ISettingsService>();
        settingsMock.SetupGet(s => s.Settings).Returns(settings);
        settingsMock.Setup(s => s.ResetToDefaultsAsync()).Returns(Task.CompletedTask);
        settingsMock.Setup(s => s.SaveSettingsAsync()).Returns(Task.CompletedTask);

        var themeMock = new Mock<IThemeService>();
        var notifMock = new Mock<INotificationService>();
        var updateMock = new Mock<IUpdateService>();

        var vm = new SettingsViewModel(settingsMock.Object, themeMock.Object, notifMock.Object, updateMock.Object);

        vm.ResetToDefaultsCommand.Execute(null);

        themeMock.Verify(t => t.ApplyTheme(ThemeMode.Light), Times.Once);
        notifMock.Verify(n => n.AddNotification(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationType>()),
            Times.Once);
    }
}