using System.Collections.ObjectModel;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Core.Interfaces;

public interface INotificationService
{
    ObservableCollection<Notification> Notifications { get; }
    int UnreadCount { get; }

    void AddNotification(string title, string message, NotificationType type = NotificationType.Info);
    void MarkAsRead(Guid notificationId);
    void MarkAllAsRead();
    void RemoveNotification(Guid notificationId);
    void ClearAll();

    event Action? NotificationsChanged;
    event Action<Notification>? NotificationAdded;
}