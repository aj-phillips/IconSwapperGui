using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Swapper;

namespace IconSwapperGui.Infrastructure.Services;

public class FolderManagementService(ILoggingService logger) : IFolderManagementService
{
    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public ObservableCollection<FolderShortcut> GetFolders(string? folderPath)
    {
        var folders = new ObservableCollection<FolderShortcut>();

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            logger.LogWarning("GetFolders called with null or empty path");
            return folders;
        }

        if (!Directory.Exists(folderPath))
        {
            logger.LogWarning($"GetFolders path does not exist: {folderPath}");
            return folders;
        }

        try
        {
            var dirs = Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly);
            foreach (var d in dirs)
            {
                folders.Add(new FolderShortcut(Path.GetFileName(d), d));
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error enumerating folders for {folderPath}", ex);
        }

        return folders;
    }

    public async Task<bool> ChangeFolderIconAsync(string folderPath, string iconPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                logger.LogWarning($"Folder does not exist: {folderPath}");
                return false;
            }

            if (!File.Exists(iconPath))
            {
                logger.LogWarning($"Icon file does not exist: {iconPath}");
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
                    logger.LogError($"Failed to clear attributes on folder before writing desktop.ini: {folderPath}",
                        ex);
                }

                if (File.Exists(iniPath))
                {
                    try
                    {
                        File.SetAttributes(iniPath, FileAttributes.Normal);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to set desktop.ini attributes to Normal before writing: {iniPath}",
                            ex);
                    }
                }

                var wrote = false;
                var attempts = 0;
                while (!wrote && attempts < 5)
                {
                    attempts++;
                    try
                    {
                        await File.WriteAllLinesAsync(iniPath, lines).ConfigureAwait(false);
                        wrote = true;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        try
                        {
                            await using var fs = new FileStream(iniPath, FileMode.Create, FileAccess.Write,
                                FileShare.ReadWrite);
                            await using var sw = new StreamWriter(fs);
                            foreach (var line in lines)
                            {
                                await sw.WriteLineAsync(line).ConfigureAwait(false);
                            }

                            wrote = true;
                        }
                        catch (Exception)
                        {
                            logger.LogWarning($"Attempt {attempts} failed writing desktop.ini, will retry: {iniPath}");
                            await Task.Delay(150).ConfigureAwait(false);
                        }
                    }
                    catch (Exception)
                    {
                        logger.LogWarning($"Attempt {attempts} failed writing desktop.ini, will retry: {iniPath}");
                        await Task.Delay(150).ConfigureAwait(false);
                    }
                }

                if (!wrote)
                {
                    logger.LogError($"Failed to write desktop.ini after multiple attempts: {iniPath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected error while preparing/writing desktop.ini: {iniPath}", ex);
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
            catch
            {
            }

            logger.LogInfo($"Folder icon changed for {folderPath} to {iconPath}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to change folder icon for {folderPath}", ex);
            return false;
        }
    }
}