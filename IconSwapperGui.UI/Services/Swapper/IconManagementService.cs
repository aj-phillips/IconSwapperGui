using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Swapper;
using IWshRuntimeLibrary;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using File = System.IO.File;

namespace IconSwapperGui.UI.Services.Swapper;

public class IconManagementService(ILoggingService logger) : IIconManagementService
{
    private readonly List<string> _supportedExtensions = new() { ".png", ".jpg", ".jpeg" };

    public IEnumerable<Icon> GetIcons(string? folderPath, IEnumerable<string> extensions)
    {
        logger.LogInfo($"GetIcons called with folderPath: {folderPath ?? "null"}");

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            logger.LogError("Folder path is null or empty");
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));
        }

        var enumerable = (extensions ?? Enumerable.Empty<string>()).ToList();
        logger.LogInfo($"Extensions requested: {string.Join(", ", enumerable)}");

        if (!enumerable.Any())
        {
            logger.LogError("Extensions parameter is null or empty");
            throw new ArgumentException("Extensions cannot be null or empty.", nameof(extensions));
        }

        var icons = new List<Icon>();

        if (!Directory.Exists(folderPath))
        {
            logger.LogError($"Directory does not exist: {folderPath}");
            throw new DirectoryNotFoundException($"Directory {folderPath} does not exist.");
        }

        try
        {
            foreach (var extension in enumerable)
            {
                var searchPattern = $"*.{extension.TrimStart('*', '.')}";
                var files = Directory.GetFiles(folderPath, searchPattern, SearchOption.AllDirectories);
                logger.LogInfo($"Found {files.Length} files with extension {extension} (including subdirectories)");

                icons.AddRange(files.Select(file => new Icon(Path.GetFileName(file), file)));
            }

            logger.LogInfo($"Successfully loaded {icons.Count} total icons from {folderPath} and subdirectories");
            return icons;
        }
        catch (IOException ex)
        {
            logger.LogError($"IOException occurred while accessing folder: {folderPath}", ex);
            throw new IOException($"An error occurred while accessing {folderPath}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            logger.LogError($"Unexpected error in GetIcons for folder: {folderPath}", ex);
            throw;
        }
    }

    public ObservableCollection<Icon> GetIcons(string? folderPath)
    {
        logger.LogInfo($"GetIcons (overload) called with folderPath: {folderPath ?? "null"}");
        var icons = new ObservableCollection<Icon>();

        if (string.IsNullOrEmpty(folderPath))
        {
            logger.LogWarning("Folder path is null or empty, returning empty collection");
            return icons;
        }

        try
        {
            var iconList = GetIcons(folderPath, _supportedExtensions);
            var addedCount = 0;

            foreach (var icon in iconList)
            {
                if (icons.Any(x => x.Path == icon.Path)) continue;
                icons.Add(icon);
                addedCount++;
            }

            logger.LogInfo($"Added {addedCount} unique icons to observable collection");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error loading icons from folder: {folderPath}", ex);
            throw;
        }

        return icons;
    }

    public ObservableCollection<Icon> FilterIcons(ObservableCollection<Icon> icons, string? filterString)
    {
        logger.LogInfo($"FilterIcons called with {icons?.Count ?? 0} icons and filter: {filterString ?? "null"}");

        if (icons == null)
        {
            logger.LogWarning("Icons collection is null, returning empty collection");
            return new ObservableCollection<Icon>();
        }

        if (string.IsNullOrEmpty(filterString))
        {
            logger.LogInfo($"No filter string provided, returning all {icons.Count} icons");
            return new ObservableCollection<Icon>(icons);
        }

        try
        {
            var filteredIcons = icons
                .Where(icon => icon.Name.Contains(filterString, StringComparison.OrdinalIgnoreCase))
                .ToList();

            logger.LogInfo(
                $"Filter applied: {filteredIcons.Count} icons match filter '{filterString}' out of {icons.Count}");

            return new ObservableCollection<Icon>(filteredIcons);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error filtering icons with filter string: {filterString}", ex);
            throw;
        }
    }

    public async Task<string?> GetCurrentIconPathAsync(string filePath)
    {
        try
        {
            var extension = Path.GetExtension(filePath)?.ToLower();

            switch (extension)
            {
                case ".lnk":
                {
                    var shell = new WshShell();
                    var shortcut = (IWshShortcut)shell.CreateShortcut(filePath);
                    var iconLocation = shortcut.IconLocation;

                    if (!string.IsNullOrEmpty(iconLocation))
                    {
                        var parts = iconLocation.Split(',');

                        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
                        {
                            return shortcut.TargetPath;
                        }

                        return iconLocation;
                    }

                    return shortcut.TargetPath;
                }
                case ".url":
                    try
                    {
                        var lines = await File.ReadAllLinesAsync(filePath).ConfigureAwait(false);
                        foreach (var line in lines)
                        {
                            if (!line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase)) continue;

                            var iconPath = line.Substring("IconFile=".Length).Trim();

                            return string.IsNullOrWhiteSpace(iconPath) ? null : iconPath;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to read .url file to get icon path: {filePath}", ex);
                    }

                    return null;
                case ".exe":
                    return filePath;
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to get current icon path for {filePath}", ex);
            return null;
        }
    }

    public async Task<bool> ChangeIconAsync(string filePath, string iconPath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning($"File not found: {filePath}");
                return false;
            }

            if (!File.Exists(iconPath))
            {
                logger.LogWarning($"Icon file not found: {iconPath}");
                return false;
            }

            var extension = Path.GetExtension(filePath)?.ToLower();

            switch (extension)
            {
                case ".lnk":
                    // Shortcut update is a synchronous COM operation
                    return ChangeShortcutIcon(filePath, iconPath);
                case ".url":
                    return await ChangeUrlIconAsync(filePath, iconPath).ConfigureAwait(false);
                case ".exe":
                    logger.LogWarning($"Cannot change icon for executable files directly: {filePath}");
                    return false;
                default:
                    logger.LogWarning($"Unsupported file type: {filePath}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to change icon for {filePath}", ex);
            return false;
        }
    }

    private bool ChangeShortcutIcon(string shortcutPath, string iconPath)
    {
        try
        {
            var shell = new WshShell();
            var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            var icoPath = iconPath;
            if (Path.GetExtension(iconPath)?.ToLower() != ".ico")
            {
                logger.LogInfo($"Non-ICO icon provided, may need conversion: {iconPath}");
            }

            shortcut.IconLocation = $"{icoPath},0";
            shortcut.Save();

            logger.LogInfo($"Successfully changed icon for shortcut: {shortcutPath}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to change shortcut icon: {shortcutPath}", ex);
            return false;
        }
    }

    private async Task<bool> ChangeUrlIconAsync(string urlPath, string iconPath)
    {
        try
        {
            var content = (await File.ReadAllLinesAsync(urlPath).ConfigureAwait(false)).ToList();
            var replaced = false;

            for (var i = 0; i < content.Count; i++)
            {
                if (content[i].StartsWith("IconFile", StringComparison.OrdinalIgnoreCase))
                {
                    content[i] = "IconFile=" + iconPath;
                    replaced = true;
                }
            }

            if (!replaced)
            {
                content.Add("IconFile=" + iconPath);
            }

            await File.WriteAllLinesAsync(urlPath, content).ConfigureAwait(false);

            logger.LogInfo($"Successfully changed icon for URL file: {urlPath}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to change URL file icon: {urlPath}", ex);
            return false;
        }
    }

    public async Task<IEnumerable<Icon>> GetIconsAsync(string? folderPath, IEnumerable<string> extensions)
    {
        // IO-bound work; perform asynchronously
        return await Task.Run(() => GetIcons(folderPath, extensions)).ConfigureAwait(false);
    }

    public async Task<ObservableCollection<Icon>> GetIconsAsync(string? folderPath)
    {
        var icons = await GetIconsAsync(folderPath, _supportedExtensions).ConfigureAwait(false);
        return new ObservableCollection<Icon>(icons);
    }

    public async Task<bool> DeleteIconAsync(string iconPath)
    {
        ArgumentNullException.ThrowIfNull(iconPath);

        try
        {
            if (!File.Exists(iconPath))
            {
                logger.LogWarning($"Icon file not found: {iconPath}");
                return false;
            }

            await Task.Run(() => File.Delete(iconPath)).ConfigureAwait(false);
            logger.LogInfo($"Successfully deleted icon: {iconPath}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to delete icon: {iconPath}", ex);
            return false;
        }
    }

    public async Task<bool> RenameIconAsync(string oldPath, string newName)
    {
        ArgumentNullException.ThrowIfNull(oldPath);
        ArgumentNullException.ThrowIfNull(newName);

        try
        {
            if (!File.Exists(oldPath))
            {
                logger.LogWarning($"Icon file not found: {oldPath}");
                return false;
            }

            var directory = Path.GetDirectoryName(oldPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                logger.LogError($"Could not determine directory for: {oldPath}");
                return false;
            }

            var extension = Path.GetExtension(oldPath);
            if (!newName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                newName += extension;
            }

            var newPath = Path.Combine(directory, newName);

            if (File.Exists(newPath))
            {
                logger.LogWarning($"A file with the new name already exists: {newPath}");
                return false;
            }

            await Task.Run(() => File.Move(oldPath, newPath)).ConfigureAwait(false);
            logger.LogInfo($"Successfully renamed icon from {oldPath} to {newPath}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to rename icon from {oldPath} to {newName}", ex);
            return false;
        }
    }
}