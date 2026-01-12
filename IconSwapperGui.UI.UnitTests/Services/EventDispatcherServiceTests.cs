using IconSwapperGui.UI.Services;

namespace IconSwapperGui.UI.UnitTests.Services;

[TestFixture]
public class EventDispatcherServiceTests
{
    [Test]
    public void Invoke_WhenNoApplication_Currently_ExecutesAction()
    {
        var svc = new EventDispatcherService();
        var executed = false;
        svc.Invoke(() => executed = true);
        Assert.That(executed, Is.True);
    }
}