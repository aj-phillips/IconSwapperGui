using IconSwapperGui.Core.Models.Swapper;
using System.Collections.ObjectModel;

namespace IconSwapperGui.Core.Interfaces;

public interface IIconManagementService
{
    IEnumerable<Icon> GetIcons(string? folderPath, IEnumerable<string> extensions);
    ObservableCollection<Icon> GetIcons(string? folderPath);
    ObservableCollection<Icon> FilterIcons(ObservableCollection<Icon> icons, string? filterString);
    Task<string?> GetCurrentIconPathAsync(string filePath);
    Task<bool> ChangeIconAsync(string filePath, string iconPath);
    Task<IEnumerable<Icon>> GetIconsAsync(string? folderPath, IEnumerable<string> extensions);
    Task<ObservableCollection<Icon>> GetIconsAsync(string? folderPath);
    Task<bool> DeleteIconAsync(string iconPath);
    Task<bool> RenameIconAsync(string oldPath, string newName);
}