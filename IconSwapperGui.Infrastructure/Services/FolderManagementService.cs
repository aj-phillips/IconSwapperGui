using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
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

    public async Task<IconOperationResult> ChangeFolderIconAsync(string folderPath, string iconPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                logger.LogWarning($"Folder does not exist: {folderPath}");
                return IconOperationResult.Failure("Folder Not Found", "The selected folder could not be found.");
            }

            if (!File.Exists(iconPath))
            {
                logger.LogWarning($"Icon file does not exist: {iconPath}");
                return IconOperationResult.Failure("Icon Not Found", "The selected replacement icon could not be found.");
            }

            if (!string.Equals(Path.GetExtension(iconPath), ".ico", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning($"Unsupported icon file type: {iconPath}");
                return IconOperationResult.Failure("Unsupported Icon", "Folder icons require an .ico file.");
            }

            var iniPath = Path.Combine(folderPath, "desktop.ini");
            var lines = new List<string>
            {
                "[.ShellClassInfo]",
                $"IconResource={iconPath},0"
            };

            try
            {
                TryPrepareFolderForDesktopIni(folderPath, iniPath);

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
                        wrote = await TryWriteDesktopIniWithSharedAccessAsync(iniPath, lines, attempts).ConfigureAwait(false);
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
                    return IconOperationResult.Failure("Folder Icon Update Failed", "Failed to write folder icon metadata.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Unexpected error while preparing/writing desktop.ini: {iniPath}", ex);
                return IconOperationResult.Failure("Folder Icon Update Failed", "Failed to write folder icon metadata.", ex.Message);
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
            return IconOperationResult.Success("Folder Icon Updated", "The folder icon was updated successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to change folder icon for {folderPath}", ex);
            return IconOperationResult.Failure("Folder Icon Update Failed", "Failed to update the folder icon.", ex.Message);
        }
    }

    public async Task<IconOperationResult> RestoreDefaultFolderIconAsync(string folderPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                logger.LogWarning($"Folder does not exist for default restore: {folderPath}");
                return IconOperationResult.Failure("Folder Not Found", "The selected folder could not be found.");
            }

            var iniPath = Path.Combine(folderPath, "desktop.ini");
            if (File.Exists(iniPath))
            {
                File.SetAttributes(iniPath, FileAttributes.Normal);
                var remainingLines = (await File.ReadAllLinesAsync(iniPath).ConfigureAwait(false))
                    .Where(line => !line.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase)
                                   && !line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase)
                                   && !line.StartsWith("IconIndex=", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (remainingLines.Any(line => !string.IsNullOrWhiteSpace(line) && line != "[.ShellClassInfo]"))
                {
                    await File.WriteAllLinesAsync(iniPath, remainingLines).ConfigureAwait(false);
                    new FileInfo(iniPath).Attributes |= FileAttributes.Hidden | FileAttributes.System;
                }
                else
                {
                    File.Delete(iniPath);
                }
            }

            var dirInfo = new DirectoryInfo(folderPath);
            dirInfo.Attributes &= ~FileAttributes.ReadOnly;
            dirInfo.Attributes &= ~FileAttributes.System;

            try
            {
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
            }

            logger.LogInfo($"Folder icon restored for {folderPath}");
            return IconOperationResult.Success("Folder Icon Restored", "The folder icon was restored to its default.");
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to restore folder icon for {folderPath}", ex);
            return IconOperationResult.Failure("Folder Icon Restore Failed", "Failed to restore the folder icon.", ex.Message);
        }
    }

    private void TryPrepareFolderForDesktopIni(string folderPath, string iniPath)
    {
        try
        {
            var dirInfoForWrite = new DirectoryInfo(folderPath);
            dirInfoForWrite.Attributes &= ~FileAttributes.ReadOnly;
            dirInfoForWrite.Attributes &= ~FileAttributes.System;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to clear attributes on folder before writing desktop.ini: {folderPath}", ex);
        }

        if (!File.Exists(iniPath)) return;

        try
        {
            File.SetAttributes(iniPath, FileAttributes.Normal);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to set desktop.ini attributes to Normal before writing: {iniPath}", ex);
        }
    }

    private async Task<bool> TryWriteDesktopIniWithSharedAccessAsync(string iniPath, IEnumerable<string> lines, int attempts)
    {
        try
        {
            await using var fs = new FileStream(iniPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            await using var sw = new StreamWriter(fs);
            foreach (var line in lines)
            {
                await sw.WriteLineAsync(line).ConfigureAwait(false);
            }

            return true;
        }
        catch (Exception)
        {
            logger.LogWarning($"Attempt {attempts} failed writing desktop.ini, will retry: {iniPath}");
            await Task.Delay(150).ConfigureAwait(false);
            return false;
        }
    }
}
