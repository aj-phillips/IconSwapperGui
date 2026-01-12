using CommunityToolkit.Mvvm.ComponentModel;

namespace IconSwapperGui.Core.Models.Swapper.IconVersionManagement;

public partial class IconVersion : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [ObservableProperty] private string _iconPath = string.Empty;
    [ObservableProperty] private string _thumbnailPath = string.Empty;
    [ObservableProperty] private DateTime _timestamp = DateTime.Now;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _isCurrent;
    [ObservableProperty] private bool _isOriginal;
    [ObservableProperty] private long _fileSize;

    public string RelativeTimeDisplay
    {
        get
        {
            var span = DateTime.Now - Timestamp;

            switch (span.TotalMinutes)
            {
                case < 1:
                    return "Just now";
                case < 60:
                    return $"{(int)span.TotalMinutes} minutes ago";
            }

            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} hours ago";

            return span.TotalDays switch
            {
                < 7 => $"{(int)span.TotalDays} days ago",
                < 30 => $"{(int)(span.TotalDays / 7)} weeks ago",
                < 365 => $"{(int)(span.TotalDays / 30)} months ago",
                _ => $"{(int)(span.TotalDays / 365)} years ago"
            };
        }
    }

    public string FileSizeDisplay
    {
        get
        {
            return FileSize switch
            {
                < 1024 => $"{FileSize} B",
                < 1024 * 1024 => $"{FileSize / 1024} KB",
                _ => $"{FileSize / (1024 * 1024)} MB"
            };
        }
    }
}