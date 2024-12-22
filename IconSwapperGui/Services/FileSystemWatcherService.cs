using System.IO;
using IconSwapperGui.Services.Interfaces;

namespace IconSwapperGui.Services;

public class FileSystemWatcherService : IFileSystemWatcherService
{
    private readonly FileSystemWatcher? _fileSystemWatcher;

    public FileSystemWatcherService(string? path, Action<object, FileSystemEventArgs> onChanged,
        Action<object, RenamedEventArgs> onRenamed)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            _fileSystemWatcher = null;
            return;
        }

        _fileSystemWatcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
        };
        _fileSystemWatcher.Created += new FileSystemEventHandler(onChanged);
        _fileSystemWatcher.Deleted += new FileSystemEventHandler(onChanged);
        _fileSystemWatcher.Renamed += new RenamedEventHandler(onRenamed);
    }

    public void StartWatching()
    {
        if (_fileSystemWatcher != null) _fileSystemWatcher.EnableRaisingEvents = true;
    }

    public void StopWatching()
    {
        if (_fileSystemWatcher != null) _fileSystemWatcher.EnableRaisingEvents = false;
    }

    public void Dispose()
    {
        _fileSystemWatcher?.Dispose();
        GC.SuppressFinalize(this);
    }
}