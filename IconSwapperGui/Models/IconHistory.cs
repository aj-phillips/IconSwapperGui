using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IconSwapperGui.Models;

public class IconHistory : INotifyPropertyChanged
{
    private string _filePath;
    private string _fileName;
    private DateTime _lastModified;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value;
            OnPropertyChanged();
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged();
        }
    }

    public DateTime LastModified
    {
        get => _lastModified;
        set
        {
            _lastModified = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<IconVersion> Versions { get; set; } = new();

    public IconVersion CurrentVersion => Versions.Count > 0 ? Versions[^1] : null;

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}