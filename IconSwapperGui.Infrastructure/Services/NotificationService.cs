using System.Collections.ObjectModel;
using System.Runtime.Versioning;
using System.Media;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IDispatcher? _dispatcher;
    private readonly ISettingsService? _settingsService;

    public NotificationService()
    {
    }

    public NotificationService(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public NotificationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public NotificationService(IDispatcher dispatcher, ISettingsService settingsService)
    {
        _dispatcher = dispatcher;
        _settingsService = settingsService;
    }

    public ObservableCollection<Notification> Notifications { get; } = new();

    public int UnreadCount => Notifications.Count(n => !n.IsRead);

    public event Action? NotificationsChanged;
    public event Action<Notification>? NotificationAdded;

    public void AddNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        if (_dispatcher != null)
        {
            _dispatcher.Invoke(Add);
            return;
        }

        Add();
        return;

        void Add()
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            Notifications.Insert(0, notification);

            try
            {
                if (ShouldPlaySound(notification.Type))
                {
                    PlaySystemSound(notification.Type);
                }
            }
            catch
            {
                // ignore sound playback errors
            }

            NotificationsChanged?.Invoke();
            NotificationAdded?.Invoke(notification);
        }
    }

    private bool ShouldPlaySound(NotificationType type)
    {
        var notifSettings = _settingsService?.Settings.Notifications;
        if (notifSettings == null || !notifSettings.PlaySound)
            return false;

        return type switch
        {
            NotificationType.Info => notifSettings.PlaySoundInfo,
            NotificationType.Success => notifSettings.PlaySoundSuccess,
            NotificationType.Warning => notifSettings.PlaySoundWarning,
            NotificationType.Error => notifSettings.PlaySoundError,
            _ => false
        };
    }

    public void MarkAsRead(Guid notificationId)
    {
        if (_dispatcher != null)
            _dispatcher.Invoke(Mark);
        else
            Mark();
        return;

        void Mark()
        {
            var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);

            if (notification == null) return;

            notification.IsRead = true;

            NotificationsChanged?.Invoke();
        }
    }

    public void MarkAllAsRead()
    {
        if (_dispatcher != null)
            _dispatcher.Invoke(MarkAll);
        else
            MarkAll();
        return;

        void MarkAll()
        {
            foreach (var notification in Notifications) notification.IsRead = true;

            NotificationsChanged?.Invoke();
        }
    }

    public void RemoveNotification(Guid notificationId)
    {
        if (_dispatcher != null)
            _dispatcher.Invoke(Remove);
        else
            Remove();
        return;

        void Remove()
        {
            var notification = Notifications.FirstOrDefault(n => n.Id == notificationId);

            if (notification == null) return;

            Notifications.Remove(notification);

            NotificationsChanged?.Invoke();
        }
    }

    public void ClearAll()
    {
        if (_dispatcher != null)
            _dispatcher.Invoke(Clear);
        else
            Clear();
        return;

        void Clear()
        {
            Notifications.Clear();
            NotificationsChanged?.Invoke();
        }
    }

    [SupportedOSPlatform("windows")]
    private static void PlaySystemSound(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Info:
            case NotificationType.Success:
                SystemSounds.Asterisk.Play();
                break;
            case NotificationType.Warning:
                SystemSounds.Exclamation.Play();
                break;
            case NotificationType.Error:
                SystemSounds.Hand.Play();
                break;
        }
    }
}