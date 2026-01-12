using IconSwapperGui.Core.Models;
using IconSwapperGui.Infrastructure.Services;

namespace IconSwapperGui.Infrastructure.UnitTests.Services;

[TestFixture]
public class NotificationServiceTests
{
    [Test]
    public void AddNotification_AddsToCollectionAndFiresEvent()
    {
        var svc = new NotificationService();
        var called = false;

        svc.NotificationsChanged += () => called = true;

        svc.AddNotification("t", "m", NotificationType.Success);

        Assert.That(svc.Notifications, Has.Count.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(svc.Notifications.First().Title, Is.EqualTo("t"));
            Assert.That(called, Is.True);
        });
    }

    [Test]
    public void MarkAsRead_SetsIsRead()
    {
        var svc = new NotificationService();

        svc.AddNotification("t", "m");

        var id = svc.Notifications.First().Id;

        svc.MarkAsRead(id);

        Assert.That(svc.Notifications.First().IsRead, Is.True);
    }

    [Test]
    public void MarkAllAsRead_SetsAllAsRead()
    {
        var svc = new NotificationService();

        svc.AddNotification("t1", "m1");
        svc.AddNotification("t2", "m2");

        svc.MarkAllAsRead();

        Assert.That(svc.Notifications.All(n => n.IsRead));
    }

    [Test]
    public void RemoveNotification_RemovesItem()
    {
        var svc = new NotificationService();

        svc.AddNotification("t1", "m1");

        var id = svc.Notifications.First().Id;

        svc.RemoveNotification(id);

        Assert.That(svc.Notifications, Is.Empty);
    }

    [Test]
    public void ClearAll_RemovesAll()
    {
        var svc = new NotificationService();

        svc.AddNotification("t1", "m1");
        svc.AddNotification("t2", "m2");

        svc.ClearAll();

        Assert.That(svc.Notifications, Is.Empty);
    }
}