using System;
using System.IO;
using IconSwapperGui.Interfaces;

namespace IconSwapperGui.Services
{
    public class FileSystemWatcherService : IFileSystemWatcherService
    {
        private readonly FileSystemWatcher _fileSystemWatcher;

        public FileSystemWatcherService(string path, Action<object, FileSystemEventArgs> onChanged,
            Action<object, RenamedEventArgs> onRenamed)
        {
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
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void StopWatching()
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}