using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.UI.ViewModels;

public partial class ToastNotificationViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _dismissTimer;
    private bool _disposed;

    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private NotificationType _type;

    public ToastNotificationViewModel()
    {
        _dismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _dismissTimer.Tick += DismissTimer_Tick;
    }

    public void ShowNotification(Notification notification)
    {
        Title = notification.Title;
        Message = notification.Message;
        Type = notification.Type;
        IsVisible = true;

        _dismissTimer.Stop();
        _dismissTimer.Start();
    }

    [RelayCommand]
    private void Dismiss()
    {
        IsVisible = false;
        _dismissTimer.Stop();
    }

    private void DismissTimer_Tick(object? sender, EventArgs e)
    {
        Dismiss();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _dismissTimer?.Stop();
        if (_dismissTimer != null)
            _dismissTimer.Tick -= DismissTimer_Tick;
    }
}
