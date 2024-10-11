namespace IconSwapperGui.Interfaces;

public interface IFileSystemWatcherService : IDisposable
{
    void StartWatching();
    void StopWatching();
}