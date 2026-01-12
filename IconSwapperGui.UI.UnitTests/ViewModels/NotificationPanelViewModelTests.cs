using System.Collections.ObjectModel;
using Moq;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.UI.ViewModels;

namespace IconSwapperGui.UI.UnitTests.ViewModels;

[TestFixture]
public class NotificationPanelViewModelTests
{
    [Test]
    public void TogglePanel_TogglesIsOpen()
    {
        var ns = new Mock<INotificationService>();
        ns.SetupGet(n => n.Notifications).Returns(new ObservableCollection<Notification>());
        var vm = new NotificationPanelViewModel(ns.Object);

        vm.TogglePanelCommand.Execute(null);
        Assert.That(vm.IsOpen, Is.True);

        vm.TogglePanelCommand.Execute(null);
        Assert.That(vm.IsOpen, Is.False);
    }

    [Test]
    public void Commands_CallServiceMethods()
    {
        var ns = new Mock<INotificationService>();
        var collection = new ObservableCollection<Notification>();
        var n = new Notification { Title = "t" };
        collection.Add(n);
        ns.SetupGet(x => x.Notifications).Returns(collection);

        var vm = new NotificationPanelViewModel(ns.Object);

        vm.MarkAsReadCommand.Execute(n.Id);
        ns.Verify(s => s.MarkAsRead(n.Id), Times.Once);

        vm.RemoveNotificationCommand.Execute(n.Id);
        ns.Verify(s => s.RemoveNotification(n.Id), Times.Once);

        vm.MarkAllAsReadCommand.Execute(null);
        ns.Verify(s => s.MarkAllAsRead(), Times.Once);

        vm.ClearAllCommand.Execute(null);
        ns.Verify(s => s.ClearAll(), Times.Once);
        Assert.That(vm.IsOpen, Is.False);
    }
}