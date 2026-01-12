using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Core.UnitTests.Models;

[TestFixture]
public class NotificationTests
{
    [Test]
    public void DefaultValues_AreSet()
    {
        var n = new Notification();

        Assert.Multiple(() =>
        {
            Assert.That(n.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(n.Title, Is.EqualTo(string.Empty));
            Assert.That(n.Message, Is.EqualTo(string.Empty));
            Assert.That(n.Type, Is.EqualTo(NotificationType.Info));
            Assert.That(n.IsRead, Is.False);
        });
    }

    [Test]
    public void RefreshTimeBindings_InvokesPropertyChanged()
    {
        var n = new Notification();

        var invoked = false;

        n.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(Notification.Timestamp)) invoked = true;
        };

        n.RefreshTimeBindings();

        Assert.That(invoked, Is.True);
    }
}