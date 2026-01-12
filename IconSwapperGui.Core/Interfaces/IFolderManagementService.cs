using System.Collections.ObjectModel;
using IconSwapperGui.Core.Models.Swapper;

namespace IconSwapperGui.Core.Interfaces;

public interface IFolderManagementService
{
    ObservableCollection<FolderShortcut> GetFolders(string? folderPath);
    Task<bool> ChangeFolderIconAsync(string folderPath, string iconPath);
}