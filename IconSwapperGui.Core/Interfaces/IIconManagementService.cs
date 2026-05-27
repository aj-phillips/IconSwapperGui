using System.Collections.ObjectModel;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Swapper;

namespace IconSwapperGui.Core.Interfaces;

public interface IIconManagementService
{
    IEnumerable<Icon> GetIcons(string? folderPath, IEnumerable<string> extensions);
    ObservableCollection<Icon> GetIcons(string? folderPath);
    ObservableCollection<Icon> FilterIcons(ObservableCollection<Icon> icons, string? filterString);
    Task<string?> GetCurrentIconPathAsync(string filePath);
    Task<IconOperationResult> ChangeIconAsync(string filePath, string iconPath);
    Task<IEnumerable<Icon>> GetIconsAsync(string? folderPath, IEnumerable<string> extensions);
    Task<ObservableCollection<Icon>> GetIconsAsync(string? folderPath);
    Task<IconOperationResult> DeleteIconAsync(string iconPath);
    Task<IconOperationResult> RenameIconAsync(string oldPath, string newName);
}
