namespace IconSwapperGui.Services.Interfaces;

public interface IFileSystemWatcherService : IDisposable
{
    void StartWatching();
    void StopWatching();
}