using System.Collections.ObjectModel;
using System.IO;
using IconSwapperGui.Models;
using IconSwapperGui.Services.Interfaces;
using Serilog;
using System.Runtime.InteropServices;

namespace IconSwapperGui.Services;

public class FolderService : IFolderService
{
    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private readonly ILogger _logger = Log.ForContext<FolderService>();

    public ObservableCollection<FolderItem> GetFolders(string? folderPath)
    {
        var folders = new ObservableCollection<FolderItem>();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            _logger.Warning("GetFolders called with null or empty path");
            return folders;
        }

        if (!Directory.Exists(folderPath))
        {
            _logger.Warning("GetFolders path does not exist: {Path}", folderPath);
            return folders;
        }

        try
        {
            var dirs = Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly);
            foreach (var d in dirs)
            {
                folders.Add(new FolderItem(Path.GetFileName(d), d));
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error enumerating folders for {Path}", folderPath);
        }

        return folders;
    }

    public Task<bool> ChangeFolderIconAsync(string folderPath, string iconPath)
    {
        return Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    _logger.Warning("Folder does not exist: {Path}", folderPath);
                    return false;
                }

                if (!File.Exists(iconPath))
                {
                    _logger.Warning("Icon file does not exist: {IconPath}", iconPath);
                    return false;
                }

                var iniPath = Path.Combine(folderPath, "desktop.ini");
                var lines = new List<string>
                {
                    "[.ShellClassInfo]",
                    $"IconResource={iconPath},0"
                };

                try
                {
                    try
                    {
                        var dirInfoForWrite = new DirectoryInfo(folderPath);
                        dirInfoForWrite.Attributes &= ~FileAttributes.ReadOnly;
                        dirInfoForWrite.Attributes &= ~FileAttributes.System;
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to clear attributes on folder before writing desktop.ini: {Path}", folderPath);
                    }

                    if (File.Exists(iniPath))
                    {
                        try
                        {
                            File.SetAttributes(iniPath, FileAttributes.Normal);
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "Failed to set desktop.ini attributes to Normal before writing: {Path}", iniPath);
                        }
                    }

                    var wrote = false;
                    var attempts = 0;
                    while (!wrote && attempts < 5)
                    {
                        attempts++;
                        try
                        {
                            File.WriteAllLines(iniPath, lines);
                            wrote = true;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            try
                            {
                                using var fs = new FileStream(iniPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                                using var sw = new StreamWriter(fs);
                                foreach (var line in lines)
                                {
                                    sw.WriteLine(line);
                                }

                                wrote = true;
                            }
                            catch (Exception ex)
                            {
                                _logger.Warning(ex, "Attempt {Attempt} failed writing desktop.ini, will retry: {Path}", attempts, iniPath);
                                System.Threading.Thread.Sleep(150);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "Attempt {Attempt} failed writing desktop.ini, will retry: {Path}", attempts, iniPath);
                            System.Threading.Thread.Sleep(150);
                        }
                    }

                    if (!wrote)
                    {
                        _logger.Error("Failed to write desktop.ini after multiple attempts: {Path}", iniPath);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unexpected error while preparing/writing desktop.ini: {Path}", iniPath);
                    return false;
                }

                var dirInfo = new DirectoryInfo(folderPath);
                dirInfo.Attributes |= FileAttributes.ReadOnly | FileAttributes.System;

                var fi = new FileInfo(iniPath);
                fi.Attributes |= FileAttributes.Hidden | FileAttributes.System;

                try
                {
                    SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
                }
                catch { }

                _logger.Information("Folder icon changed for {Path} to {Icon}", folderPath, iconPath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to change folder icon for {Path}", folderPath);
                return false;
            }
        });
    }
}
