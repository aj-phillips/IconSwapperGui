using Moq;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Settings;
using IconSwapperGui.UI.Services;

namespace IconSwapperGui.UI.UnitTests.Services;

[TestFixture]
public class ThemeApplierTests
{
    [Test]
    public void Dispose_UnsubscribesFromThemeChanged()
    {
        var themeMock = new Mock<IThemeService>();
        var settingsMock = new Mock<ISettingsService>();
        var dispatcherMock = new Mock<IDispatcher>();
        dispatcherMock.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

        themeMock.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Light);

        var applier = new ThemeApplier(themeMock.Object, settingsMock.Object, dispatcherMock.Object);

        applier.Dispose();

        themeMock.Raise(t => t.ThemeChanged += null);

        dispatcherMock.Verify(d => d.Invoke(It.IsAny<Action>()), Times.AtLeastOnce);
    }
}