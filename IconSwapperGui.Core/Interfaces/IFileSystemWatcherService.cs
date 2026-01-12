namespace IconSwapperGui.Core.Interfaces;

public interface IFileSystemWatcherService
{
    void StartWatching();
    void StopWatching();
    void Dispose();
}