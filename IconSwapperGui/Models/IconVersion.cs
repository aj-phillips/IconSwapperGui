using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IconSwapperGui.Models;

public class IconVersion : INotifyPropertyChanged
{
    private string _iconPath;
    private string _thumbnailPath;
    private DateTime _timestamp;
    private string _description;
    private bool _isCurrent;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string IconPath
    {
        get => _iconPath;
        set
        {
            _iconPath = value;
            OnPropertyChanged();
        }
    }

    public string ThumbnailPath
    {
        get => _thumbnailPath;
        set
        {
            _thumbnailPath = value;
            OnPropertyChanged();
        }
    }

    public DateTime Timestamp
    {
        get => _timestamp;
        set
        {
            _timestamp = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TimestampDisplay));
            OnPropertyChanged(nameof(RelativeTime));
        }
    }

    public string TimestampDisplay => Timestamp.ToString("MMM dd, yyyy HH:mm");

    public string RelativeTime
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

    public string Description
    {
        get => _description;
        set
        {
            _description = value;
            OnPropertyChanged();
        }
    }

    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            _isCurrent = value;
            OnPropertyChanged();
        }
    }

    public long FileSize { get; set; }

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

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}