using CommunityToolkit.Mvvm.ComponentModel;

namespace IconSwapperGui.Core.Models;

public partial class Notification : ObservableObject
{
    [ObservableProperty] private Guid _id = Guid.NewGuid();

    [ObservableProperty] private bool _isRead;

    [ObservableProperty] private string _message = string.Empty;

    [ObservableProperty] private DateTime _timestamp = DateTime.Now;

    [ObservableProperty] private string _title = string.Empty;

    [ObservableProperty] private NotificationType _type = NotificationType.Info;
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}