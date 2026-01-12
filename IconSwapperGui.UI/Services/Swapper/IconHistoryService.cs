using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Swapper.IconVersionManagement;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using SD = System.Drawing;
using SD2D = System.Drawing.Drawing2D;
using SDImaging = System.Drawing.Imaging;

namespace IconSwapperGui.UI.Services.Swapper;

public class IconHistoryService : IIconHistoryService
{
    private readonly IIconManagementService _iconManagementService;
    private readonly ILoggingService _logger;

    private readonly string _historyDirectory;
    private readonly string _thumbnailDirectory;
    private readonly string _historyFilePath;
    private readonly ConcurrentDictionary<string, IconHistory> _historyCache;

    private readonly Task _initialLoadTask;
    private const int MaxVersionsPerFile = 50;
    private const int ThumbnailSize = 256;

    public IconHistoryService(IIconManagementService iconManagementService, ILoggingService logger)
    {
        ArgumentNullException.ThrowIfNull(iconManagementService);
        ArgumentNullException.ThrowIfNull(logger);

        _iconManagementService = iconManagementService;
        _logger = logger;

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
            _logger.LogInfo($"Recording icon change for {filePath} with icon {iconPath}");

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required", nameof(filePath));

            var normalizedPath = NormalizePath(filePath);
            var history = _historyCache.GetOrAdd(normalizedPath, _ => new IconHistory
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                LastModified = DateTime.Now
            });

            await TryPreserveOriginalIconAsync(history, filePath, iconPath);

            var (iconFilePath, iconIndex) = ParseIconPath(iconPath);
            iconFilePath = string.IsNullOrWhiteSpace(iconFilePath) ? filePath : iconFilePath;

            var thumbnailPath = await GetThumbnailForPathAsync(iconFilePath, iconIndex);
            var version = CreateVersion(iconPath, filePath, thumbnailPath);

            if (!history.Versions.Any())
                version.IsOriginal = true;

            MarkAllNonCurrent(history);
            history.Versions.Add(version);
            history.LastModified = DateTime.Now;

            TrimToMaxVersions(history);

            await SaveHistoryAsync();

            _logger.LogInfo($"Icon change recorded successfully. Total versions: {history.Versions.Count}");

            return version;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to record icon change for {filePath}", ex);
            throw;
        }
    }

    public async Task<IconHistory> GetHistoryAsync(string filePath)
    {
        var normalizedPath = NormalizePath(filePath);

        if (_historyCache.TryGetValue(normalizedPath, out var history))
            return history;

        try
        {
            await _initialLoadTask.ConfigureAwait(false);
        }
        catch
        {
            // best-effort load
        }

        return _historyCache.TryGetValue(normalizedPath, out history) ? history : null;
    }

    public async Task<IEnumerable<IconHistory>> GetAllHistoriesAsync()
    {
        return await Task.FromResult(_historyCache.Values.OrderByDescending(h => h.LastModified).ToList());
    }

    public async Task<bool> RevertToVersionAsync(string filePath, Guid versionId)
    {
        try
        {
            _logger.LogInfo($"Reverting {filePath} to version {versionId}");

            var history = await GetHistoryAsync(filePath);
            if (history == null)
            {
                _logger.LogWarning($"No history found for {filePath}");
                return false;
            }

            var version = history.Versions.FirstOrDefault(v => v.Id == versionId);
            if (version == null)
            {
                _logger.LogWarning($"Version {versionId} not found");
                return false;
            }

            var success = await _iconManagementService.ChangeIconAsync(filePath, version.IconPath);

            if (success)
            {
                await RecordIconChangeAsync(filePath, version.IconPath);
                _logger.LogInfo($"Successfully reverted to version {versionId}");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to revert to version {versionId}", ex);
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
                _logger.LogWarning("Cannot delete current version");
                return false;
            }

            history.Versions.Remove(version);
            CleanupThumbnail(version.ThumbnailPath);

            await SaveHistoryAsync();

            _logger.LogInfo($"Deleted version {versionId}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to delete version {versionId}", ex);
            return false;
        }
    }

    public async Task<bool> ClearHistoryAsync(string filePath)
    {
        try
        {
            var normalizedPath = NormalizePath(filePath);

            if (!_historyCache.TryGetValue(normalizedPath, out var history) || history == null)
                return false;

            var toRemove = history.Versions.Where(v => !v.IsOriginal).ToList();
            if (toRemove.Count == 0)
                return false;

            foreach (var version in toRemove)
            {
                history.Versions.Remove(version);
                CleanupThumbnail(version.ThumbnailPath);
            }

            history.LastModified = DateTime.Now;

            await SaveHistoryAsync();

            _logger.LogInfo($"Cleared non-original history for {filePath}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to clear history for {filePath}", ex);
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
                    if (version.IsOriginal)
                        continue;

                    CleanupThumbnail(version.ThumbnailPath);
                }
            }

            _historyCache.Where(x => x.Value.Versions.Any(v => !v.IsOriginal)).ToList()
                .ForEach(x => _historyCache.TryRemove(x.Key, out _));

            // Remove entries from icon_history file
            await SaveHistoryAsync();

            _logger.LogInfo("Cleared all history");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to clear all history", ex);
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

                DestroyIcon(largeIcons[0]);

                if (smallIcons[0] != IntPtr.Zero) DestroyIcon(smallIcons[0]);

                return tempIconPath;
            }
        }
        catch
        {
        }

        return null;
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge,
        IntPtr[]? phiconSmall, uint nIcons);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).ToLowerInvariant();
    }

    private static (string? Path, int Index) ParseIconPath(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
            return (null, 0);

        var commaIndex = iconPath.IndexOf(',');
        if (commaIndex < 0)
            return (iconPath, 0);

        var filePath = iconPath[..commaIndex];
        var indexPart = iconPath[(commaIndex + 1)..];
        return int.TryParse(indexPart, out var index) ? (filePath, index) : (filePath, 0);
    }

    private async Task TryPreserveOriginalIconAsync(IconHistory history, string filePath, string iconPath)
    {
        if (history.Versions.Count != 0)
            return;

        try
        {
            var currentIcon = await _iconManagementService.GetCurrentIconPathAsync(filePath);
            if (string.IsNullOrWhiteSpace(currentIcon) || currentIcon == iconPath)
                return;

            var (curPath, curIndex) = ParseIconPath(currentIcon);
            curPath = string.IsNullOrWhiteSpace(curPath) ? filePath : curPath;

            var originalThumbnail = await GetThumbnailForPathAsync(curPath, curIndex);
            if (string.IsNullOrWhiteSpace(originalThumbnail))
                return;

            var originalVersion = new IconVersion
            {
                IconPath = currentIcon,
                ThumbnailPath = originalThumbnail,
                Timestamp = DateTime.Now.AddMilliseconds(-1),
                Description = "Original icon",
                IsCurrent = false,
                IsOriginal = true,
                FileSize = TryGetFileSize(originalThumbnail)
            };

            history.Versions.Add(originalVersion);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to preserve original icon for {filePath}", ex);
        }
    }

    private static void MarkAllNonCurrent(IconHistory history)
    {
        foreach (var v in history.Versions)
            v.IsCurrent = false;
    }

    private IconVersion CreateVersion(string iconPath, string filePath, string thumbnailPath)
    {
        var fileNameForDescription = !string.IsNullOrWhiteSpace(iconPath)
            ? Path.GetFileName(iconPath)
            : Path.GetFileName(filePath);

        return new IconVersion
        {
            IconPath = iconPath,
            ThumbnailPath = thumbnailPath,
            Timestamp = DateTime.Now,
            Description = $"Changed to {fileNameForDescription}",
            IsCurrent = true,
            FileSize = TryGetFileSize(thumbnailPath)
        };
    }

    private static long TryGetFileSize(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                return new FileInfo(path).Length;
        }
        catch
        {
        }

        return 0;
    }

    private void TrimToMaxVersions(IconHistory history)
    {
        if (history.Versions.Count <= MaxVersionsPerFile)
            return;

        var toRemove = history.Versions.Take(history.Versions.Count - MaxVersionsPerFile).ToList();
        foreach (var old in toRemove)
        {
            history.Versions.Remove(old);
            CleanupThumbnail(old.ThumbnailPath);
        }
    }

    private async Task<string> GetThumbnailForPathAsync(string pathToUse, int index)
    {
        try
        {
            var extension = Path.GetExtension(pathToUse)?.ToLowerInvariant();

            if (File.Exists(pathToUse) && extension is ".exe" or ".dll")
            {
                try
                {
                    var extractedPath = index != 0 ? ExtractIconByIndex(pathToUse, index) : null;
                    if (!string.IsNullOrWhiteSpace(extractedPath))
                        return await CreateThumbnailAndCleanupTempAsync(extractedPath);

                    using var icon = System.Drawing.Icon.ExtractAssociatedIcon(pathToUse);
                    if (icon == null)
                        return await CreateThumbnailAsync(pathToUse);

                    var tempIconPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ico");
                    await using (var fs = new FileStream(tempIconPath, FileMode.Create, FileAccess.Write))
                    {
                        icon.Save(fs);
                    }

                    return await CreateThumbnailAndCleanupTempAsync(tempIconPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        $"Failed to extract icon from {pathToUse}, falling back to direct thumbnail creation", ex);
                    return await CreateThumbnailAsync(pathToUse);
                }
            }

            return await CreateThumbnailAsync(pathToUse);
        }
        catch (Exception ex)
        {
            _logger.LogError($"GetThumbnailForPathAsync fallback for {pathToUse}", ex);
            return pathToUse;
        }
    }

    private async Task<string> CreateThumbnailAndCleanupTempAsync(string tempIconPath)
    {
        try
        {
            return await CreateThumbnailAsync(tempIconPath);
        }
        finally
        {
            try
            {
                File.Delete(tempIconPath);
            }
            catch
            {
            }
        }
    }

    private async Task<string> CreateThumbnailAsync(string iconPath)
    {
        var thumbnailFileName = $"{Guid.NewGuid()}.png";
        var thumbnailPath = Path.Combine(_thumbnailDirectory, thumbnailFileName);

        try
        {
            var ext = Path.GetExtension(iconPath)?.ToLowerInvariant();

            if (ext is ".ico" or ".exe" or ".dll")
            {
                using var icon = LoadIcon(iconPath, ext);
                if (icon != null)
                {
                    using var iconBmp = icon.ToBitmap();
                    RenderThumbnail(iconBmp, thumbnailPath);
                    return thumbnailPath;
                }
            }

            using var img = SD.Image.FromFile(iconPath);
            RenderThumbnail(img, thumbnailPath);
            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to create thumbnail for {iconPath}, falling back to copy", ex);
            return TryCopyAsThumbnail(iconPath, thumbnailPath);
        }
    }

    private static SD.Icon? LoadIcon(string iconPath, string? ext)
    {
        try
        {
            return ext == ".ico"
                ? new SD.Icon(iconPath, ThumbnailSize, ThumbnailSize)
                : SD.Icon.ExtractAssociatedIcon(iconPath);
        }
        catch
        {
            return null;
        }
    }

    private static void RenderThumbnail(SD.Image source, string thumbnailPath)
    {
        using var bmp = new SD.Bitmap(ThumbnailSize, ThumbnailSize, SDImaging.PixelFormat.Format32bppArgb);
        using var g = SD.Graphics.FromImage(bmp);

        g.Clear(SD.Color.Transparent);
        g.CompositingMode = SD2D.CompositingMode.SourceCopy;
        g.CompositingQuality = SD2D.CompositingQuality.HighQuality;
        g.InterpolationMode = SD2D.InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = SD2D.PixelOffsetMode.HighQuality;
        g.SmoothingMode = SD2D.SmoothingMode.HighQuality;

        var srcW = source.Width;
        var srcH = source.Height;
        var scale = Math.Min((float)ThumbnailSize / srcW, (float)ThumbnailSize / srcH);
        var destW = (int)(srcW * scale);
        var destH = (int)(srcH * scale);
        var destX = (ThumbnailSize - destW) / 2;
        var destY = (ThumbnailSize - destH) / 2;

        g.DrawImage(source, new SD.Rectangle(destX, destY, destW, destH));
        bmp.Save(thumbnailPath, SDImaging.ImageFormat.Png);
    }

    private string TryCopyAsThumbnail(string sourcePath, string thumbnailPath)
    {
        try
        {
            File.Copy(sourcePath, thumbnailPath, true);
            return thumbnailPath;
        }
        catch (Exception copyEx)
        {
            _logger.LogError($"Failed to copy file for thumbnail fallback: {sourcePath}", copyEx);
            return sourcePath;
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
            _logger.LogError($"Failed to cleanup thumbnail {thumbnailPath}", ex);
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
            {
                _logger.LogInfo("No history file found, starting fresh");
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

                _logger.LogInfo($"Loaded {histories.Count} history entries");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to load history from disk", ex);
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
            _logger.LogInfo("History saved to disk");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to save history to disk", ex);
        }
    }

    #endregion
}