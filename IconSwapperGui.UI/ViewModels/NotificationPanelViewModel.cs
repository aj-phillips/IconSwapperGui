using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.UI.ViewModels;

public partial class NotificationPanelViewModel : ObservableObject, IDisposable
{
    private readonly INotificationService _notificationService;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    [ObservableProperty] private bool _isOpen;

    public NotificationPanelViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
        _notificationService.NotificationsChanged += OnNotificationsChanged;
        _notificationService.NotificationAdded += OnNotificationAdded;
        
        Toast = new ToastNotificationViewModel();
        
        _timer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            // refresh frequently so the UI updates seconds -> minutes transitions
            Interval = TimeSpan.FromSeconds(10)
        };
        _timer.Tick += Timer_Tick;
    }

    public ObservableCollection<Notification> Notifications => _notificationService.Notifications;

    public int UnreadCount => _notificationService.UnreadCount;

    public bool HasNotifications => Notifications.Any();

    public ToastNotificationViewModel Toast { get; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= Timer_Tick;
        }

        if (_notificationService != null)
        {
            _notificationService.NotificationsChanged -= OnNotificationsChanged;
            _notificationService.NotificationAdded -= OnNotificationAdded;
        }

        Toast?.Dispose();
    }

    [RelayCommand]
    private void TogglePanel()
    {
        IsOpen = !IsOpen;
    }

    [RelayCommand]
    private void MarkAsRead(Guid id)
    {
        _notificationService.MarkAsRead(id);
    }

    [RelayCommand]
    private void RemoveNotification(Guid id)
    {
        _notificationService.RemoveNotification(id);
    }

    [RelayCommand]
    private void MarkAllAsRead()
    {
        _notificationService.MarkAllAsRead();
    }

    [RelayCommand]
    private void ClearAll()
    {
        _notificationService.ClearAll();
        IsOpen = false;
    }

    private void OnNotificationsChanged()
    {
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(HasNotifications));
    }

    private void OnNotificationAdded(Notification notification)
    {
        Toast.ShowNotification(notification);
    }

    partial void OnIsOpenChanged(bool value)
    {
        if (value)
        {
            if (!_timer.IsEnabled)
                _timer.Start();
            // refresh immediately when opened so timestamps update right away
            Timer_Tick(null, EventArgs.Empty);
        }
        else
        {
            if (_timer.IsEnabled)
                _timer.Stop();
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        foreach (var n in Notifications.ToList()) n.RefreshTimeBindings();
    }
}