using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IconSwapperGui.Core.Models.Swapper.IconVersionManagement;

public partial class IconHistory : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private DateTime _lastModified = DateTime.Now;

    public ObservableCollection<IconVersion> Versions { get; set; } = new();

    public IconVersion? CurrentVersion => Versions.Count > 0 ? Versions[^1] : null;
}