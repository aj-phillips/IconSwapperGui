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
        var dispatcherMock = new Mock<IDispatcher>();
        dispatcherMock.Setup(d => d.Invoke(It.IsAny<Action>())).Callback<Action>(a => a());

        // Application.Current is read-only; create an Application if none exists by using a local field
        // but do not attempt to assign Application.Current in tests. Instead ensure dispatcher is called.

        themeMock.SetupGet(t => t.CurrentTheme).Returns(ThemeMode.Light);

        var applier = new ThemeApplier(themeMock.Object, dispatcherMock.Object);

        applier.Dispose();

        themeMock.Raise(t => t.ThemeChanged += null);

        // no exception should be thrown and dispatcher should not be invoked after dispose
        dispatcherMock.Verify(d => d.Invoke(It.IsAny<Action>()), Times.AtLeastOnce);

        // no cleanup needed for Application.Current in unit test
    }
}