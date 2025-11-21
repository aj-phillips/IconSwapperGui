using System.Collections.ObjectModel;
using IconSwapperGui.Models;

namespace IconSwapperGui.Services.Interfaces;

public interface IFolderService
{
    ObservableCollection<FolderItem> GetFolders(string? folderPath);
    Task<bool> ChangeFolderIconAsync(string folderPath, string iconPath);
}
