using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using IconSwapperGui.Models;
using IconSwapperGui.Services.Interfaces;
using Serilog;
using System.Runtime.InteropServices;

namespace IconSwapperGui.Services;

public class IconHistoryService : IIconHistoryService
{
    private readonly IIconManagementService _iconManagementService;
    private readonly string _historyDirectory;
    private readonly string _thumbnailDirectory;
    private readonly string _historyFilePath;
    private readonly ConcurrentDictionary<string, IconHistory> _historyCache;
    private readonly ILogger _logger = Log.ForContext<IconHistoryService>();
    private readonly Task _initialLoadTask;

    private const int MaxVersionsPerFile = 50;

    public IconHistoryService(IIconManagementService iconManagementService)
    {
        _iconManagementService =
            iconManagementService ?? throw new ArgumentNullException(nameof(iconManagementService));
        
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IconSwapperGui");

        _historyDirectory = Path.Combine(appDataPath, "History");
        _thumbnailDirectory = Path.Combine(_historyDirectory, "Thumbnails");
        _historyFilePath = Path.Combine(_historyDirectory, "icon_history.json");

        Directory.CreateDirectory(_historyDirectory);
        Directory.CreateDirectory(_thumbnailDirectory);

        _historyCache = new ConcurrentDictionary<string, IconHistory>();

        _initialLoadTask = LoadHistoryAsync();
    }

    public async Task<IconVersion> RecordIconChangeAsync(string filePath, string iconPath)
    {
        try
        {
            _logger.Information("Recording icon change for {FilePath} with icon {IconPath}", filePath, iconPath);

            var normalizedPath = NormalizePath(filePath);
            var history = _historyCache.GetOrAdd(normalizedPath, _ => new IconHistory
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                LastModified = DateTime.Now
            });
            
            string thumbnailPath;

            var pathToUse = iconPath;
            var index = 0;

            if (!string.IsNullOrEmpty(iconPath) && iconPath.Contains(","))
            {
                var parts = iconPath.Split(',');
                pathToUse = parts[0];
                if (parts.Length > 1 && int.TryParse(parts[1], out var parsedIndex))
                    index = parsedIndex;
            }

            var extension = Path.GetExtension(pathToUse)?.ToLowerInvariant();
            if (File.Exists(pathToUse) && extension is ".exe" or ".dll")
            {
                try
                {
                    string? extracted = null;
                    if (index != 0)
                    {
                        extracted = ExtractIconByIndex(pathToUse, index);
                    }

                    if (extracted == null)
                    {
                        using var icon = System.Drawing.Icon.ExtractAssociatedIcon(pathToUse);
                        if (icon != null)
                        {
                            var tempIconPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ico");
                            await using (var fs = new FileStream(tempIconPath, FileMode.Create, FileAccess.Write))
                            {
                                icon.Save(fs);
                            }

                            thumbnailPath = await CreateThumbnailAsync(tempIconPath);
                            try { File.Delete(tempIconPath); } catch { }
                        }
                        else
                        {
                            thumbnailPath = await CreateThumbnailAsync(pathToUse);
                        }
                    }
                    else
                    {
                        thumbnailPath = await CreateThumbnailAsync(extracted);
                        try { File.Delete(extracted); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to extract icon from {PathToUse}, falling back to direct thumbnail creation", pathToUse);
                    thumbnailPath = await CreateThumbnailAsync(pathToUse);
                }
            }
            else
            {
                thumbnailPath = await CreateThumbnailAsync(pathToUse);
            }
            
            var version = new IconVersion
            {
                IconPath = iconPath,
                ThumbnailPath = thumbnailPath,
                Timestamp = DateTime.Now,
                Description = $"Changed to {Path.GetFileName(iconPath)}",
                IsCurrent = true,
                FileSize = new FileInfo(thumbnailPath).Length
            };
            
            foreach (var v in history.Versions)
            {
                v.IsCurrent = false;
            }
            
            history.Versions.Add(version);
            history.LastModified = DateTime.Now;
            
            if (history.Versions.Count > MaxVersionsPerFile)
            {
                var toRemove = history.Versions.Take(history.Versions.Count - MaxVersionsPerFile).ToList();
                foreach (var old in toRemove)
                {
                    history.Versions.Remove(old);
                    CleanupThumbnail(old.ThumbnailPath);
                }
            }
            
            await SaveHistoryAsync();

            _logger.Information("Icon change recorded successfully. Total versions: {Count}", history.Versions.Count);

            return version;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to record icon change for {FilePath}", filePath);
            throw;
        }
    }

    public async Task<IconHistory> GetHistoryAsync(string filePath)
    {
        var normalizedPath = NormalizePath(filePath);

        if (_historyCache.TryGetValue(normalizedPath, out var history))
        {
            return await Task.FromResult(history);
        }

        try
        {
            await _initialLoadTask.ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }

        if (_historyCache.TryGetValue(normalizedPath, out history))
            return history;

        return null;
    }

    public async Task<IEnumerable<IconHistory>> GetAllHistoriesAsync()
    {
        return await Task.FromResult(_historyCache.Values.OrderByDescending(h => h.LastModified));
    }

    public async Task<bool> RevertToVersionAsync(string filePath, Guid versionId)
    {
        try
        {
            _logger.Information("Reverting {FilePath} to version {VersionId}", filePath, versionId);

            var history = await GetHistoryAsync(filePath);
            if (history == null)
            {
                _logger.Warning("No history found for {FilePath}", filePath);
                return false;
            }

            var version = history.Versions.FirstOrDefault(v => v.Id == versionId);
            if (version == null)
            {
                _logger.Warning("Version {VersionId} not found", versionId);
                return false;
            }
            
            var success = await _iconManagementService.ChangeIconAsync(filePath, version.IconPath);

            if (success)
            {
                await RecordIconChangeAsync(filePath, version.IconPath);
                _logger.Information("Successfully reverted to version {VersionId}", versionId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to revert to version {VersionId}", versionId);
            return false;
        }
    }

    public async Task<bool> DeleteVersionAsync(string filePath, Guid versionId)
    {
        try
        {
            var history = await GetHistoryAsync(filePath);
            if (history == null) return false;

            var version = history.Versions.FirstOrDefault(v => v.Id == versionId);
            if (version == null) return false;
            
            if (version.IsCurrent)
            {
                _logger.Warning("Cannot delete current version");
                return false;
            }

            history.Versions.Remove(version);
            CleanupThumbnail(version.ThumbnailPath);

            await SaveHistoryAsync();

            _logger.Information("Deleted version {VersionId}", versionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete version {VersionId}", versionId);
            return false;
        }
    }

    public async Task<bool> ClearHistoryAsync(string filePath)
    {
        try
        {
            var normalizedPath = NormalizePath(filePath);

            if (!_historyCache.TryRemove(normalizedPath, out var history)) return false;

            foreach (var version in history.Versions)
            {
                CleanupThumbnail(version.ThumbnailPath);
            }

            await SaveHistoryAsync();

            _logger.Information("Cleared history for {FilePath}", filePath);

            return true;

        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to clear history for {FilePath}", filePath);
            return false;
        }
    }

    public async Task<bool> ClearAllHistoryAsync()
    {
        try
        {
            foreach (var history in _historyCache.Values)
            {
                foreach (var version in history.Versions)
                {
                    CleanupThumbnail(version.ThumbnailPath);
                }
            }

            _historyCache.Clear();
            
            if (File.Exists(_historyFilePath))
            {
                File.Delete(_historyFilePath);
            }

            _logger.Information("Cleared all history");
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to clear all history");
            return false;
        }
    }

    public async Task<int> GetVersionCountAsync(string filePath)
    {
        var history = await GetHistoryAsync(filePath);
        return history?.Versions.Count ?? 0;
    }

    public async Task<bool> HasHistoryAsync(string filePath)
    {
        var count = await GetVersionCountAsync(filePath);
        return count > 0;
    }

    #region Private Helper Methods

    // Attempt to extract a specific icon index from a file (exe/dll) using Win32 ExtractIconEx and save to a temp .ico
    private static string? ExtractIconByIndex(string filePath, int index)
    {
        try
        {
            var largeIcons = new IntPtr[1];
            var smallIcons = new IntPtr[1];
            var count = ExtractIconEx(filePath, index, largeIcons, smallIcons, 1);

            if (count > 0 && largeIcons[0] != IntPtr.Zero)
            {
                using var icon = System.Drawing.Icon.FromHandle(largeIcons[0]);
                var tempIconPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ico");
                using (var fs = new FileStream(tempIconPath, FileMode.Create, FileAccess.Write))
                {
                    icon.Save(fs);
                }

                // destroy extracted icon handles
                DestroyIcon(largeIcons[0]);
                if (smallIcons[0] != IntPtr.Zero) DestroyIcon(smallIcons[0]);

                return tempIconPath;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private string NormalizePath(string path)
    {
        return Path.GetFullPath(path).ToLowerInvariant();
    }

    private async Task<string> CreateThumbnailAsync(string iconPath)
    {
        try
        {
            var thumbnailFileName = $"{Guid.NewGuid()}.png";
            var thumbnailPath = Path.Combine(_thumbnailDirectory, thumbnailFileName);
            
            await Task.Run(() => File.Copy(iconPath, thumbnailPath, true)).ConfigureAwait(false);

            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create thumbnail for {IconPath}", iconPath);
            return iconPath;
        }
    }

    private void CleanupThumbnail(string thumbnailPath)
    {
        try
        {
            if (!string.IsNullOrEmpty(thumbnailPath) && File.Exists(thumbnailPath))
            {
                File.Delete(thumbnailPath);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to cleanup thumbnail {ThumbnailPath}", thumbnailPath);
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                _logger.Information("No history file found, starting fresh");
                return;
            }

            var json = await File.ReadAllTextAsync(_historyFilePath).ConfigureAwait(false);
            var histories = JsonSerializer.Deserialize<List<IconHistory>>(json);

            if (histories != null)
            {
                foreach (var history in histories)
                {
                    var normalizedPath = NormalizePath(history.FilePath);
                    _historyCache[normalizedPath] = history;
                }

                _logger.Information("Loaded {Count} history entries", histories.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load history from disk");
        }
    }

    private async Task SaveHistoryAsync()
    {
        try
        {
            var histories = _historyCache.Values.ToList();
            var json = JsonSerializer.Serialize(histories, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_historyFilePath, json).ConfigureAwait(false);
            _logger.Debug("History saved to disk");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save history to disk");
        }
    }

    #endregion
}