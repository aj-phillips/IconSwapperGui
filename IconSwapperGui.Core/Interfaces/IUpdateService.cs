using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Core.Interfaces;

public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdatesAsync();
    Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo);
    string GetCurrentVersion();
}