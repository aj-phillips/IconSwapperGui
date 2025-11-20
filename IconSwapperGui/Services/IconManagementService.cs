using System.Collections.ObjectModel;
using System.IO;
using IconSwapperGui.Models;
using IconSwapperGui.Services.Interfaces;
using IWshRuntimeLibrary;
using Serilog;
using File = System.IO.File;

namespace IconSwapperGui.Services;

public class IconManagementService : IIconManagementService
{
    private readonly List<string> _supportedExtensions = [".png", ".jpg", ".jpeg"];
    private readonly ILogger _logger = Log.ForContext<IconManagementService>();

    public IEnumerable<Icon> GetIcons(string? folderPath, IEnumerable<string> extensions)
    {
        _logger.Information("GetIcons called with folderPath: {FolderPath}", folderPath ?? "null");

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            _logger.Error("Folder path is null or empty");
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));
        }

        var enumerable = extensions.ToList();
        _logger.Information("Extensions requested: {Extensions}", string.Join(", ", enumerable));

        if (extensions == null || !enumerable.Any())
        {
            _logger.Error("Extensions parameter is null or empty");
            throw new ArgumentException("Extensions cannot be null or empty.", nameof(extensions));
        }

        var icons = new List<Icon>();

        if (!Directory.Exists(folderPath))
        {
            _logger.Error("Directory does not exist: {FolderPath}", folderPath);
            throw new DirectoryNotFoundException($"Directory {folderPath} does not exist.");
        }

        try
        {
            foreach (var extension in enumerable)
            {
                var searchPattern = $"*.{extension.TrimStart('*', '.')}";
                var files = Directory.GetFiles(folderPath, searchPattern);
                _logger.Information("Found {Count} files with extension {Extension}", files.Length, extension);

                icons.AddRange(files.Select(file => new Icon(Path.GetFileName(file), file)));
            }

            _logger.Information("Successfully loaded {Count} total icons from {FolderPath}", icons.Count, folderPath);
            return icons;
        }
        catch (IOException ex)
        {
            _logger.Error(ex, "IOException occurred while accessing folder: {FolderPath}", folderPath);
            throw new IOException($"An error occurred while accessing {folderPath}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error in GetIcons for folder: {FolderPath}", folderPath);
            throw;
        }
    }

    public ObservableCollection<Icon> GetIcons(string? folderPath)
    {
        _logger.Information("GetIcons (overload) called with folderPath: {FolderPath}", folderPath ?? "null");
        var icons = new ObservableCollection<Icon>();

        if (string.IsNullOrEmpty(folderPath))
        {
            _logger.Warning("Folder path is null or empty, returning empty collection");
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

            _logger.Information("Added {AddedCount} unique icons to observable collection", addedCount);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading icons from folder: {FolderPath}", folderPath);
            throw;
        }

        return icons;
    }

    public ObservableCollection<Icon> FilterIcons(ObservableCollection<Icon> icons, string? filterString)
    {
        _logger.Information("FilterIcons called with {TotalIcons} icons and filter: {FilterString}",
            icons?.Count ?? 0, filterString ?? "null");

        if (icons == null)
        {
            _logger.Warning("Icons collection is null, returning empty collection");
            return new ObservableCollection<Icon>();
        }

        if (string.IsNullOrEmpty(filterString))
        {
            _logger.Information("No filter string provided, returning all {Count} icons", icons.Count);
            return new ObservableCollection<Icon>(icons);
        }

        try
        {
            var filteredIcons = icons
                .Where(icon => icon.Name.Contains(filterString, StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.Information(
                "Filter applied: {FilteredCount} icons match filter '{FilterString}' out of {TotalCount}",
                filteredIcons.Count, filterString, icons.Count);

            return new ObservableCollection<Icon>(filteredIcons);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error filtering icons with filter string: {FilterString}", filterString);
            throw;
        }
    }

    public Task<string?> GetCurrentIconPathAsync(string filePath)
    {
        return Task.Run(() =>
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

                        return !string.IsNullOrEmpty(iconLocation) ? iconLocation : shortcut.TargetPath;
                    }
                    case ".url":
                        try
                        {
                            var lines = File.ReadAllLines(filePath);
                            foreach (var line in lines)
                            {
                                if (!line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase)) continue;

                                var iconPath = line.Substring("IconFile=".Length).Trim();

                                return string.IsNullOrWhiteSpace(iconPath) ? null : iconPath;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Failed to read .url file to get icon path: {FilePath}", filePath);
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
                Log.Error(ex, "Failed to get current icon path for {FilePath}", filePath);
                return null;
            }
        });
    }

    public Task<bool> ChangeIconAsync(string filePath, string iconPath)
    {
        return Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Log.Warning("File not found: {FilePath}", filePath);
                    return false;
                }

                if (!File.Exists(iconPath))
                {
                    Log.Warning("Icon file not found: {IconPath}", iconPath);
                    return false;
                }

                var extension = Path.GetExtension(filePath)?.ToLower();

                switch (extension)
                {
                    case ".lnk":
                        return ChangeShortcutIcon(filePath, iconPath);
                    case ".url":
                        return ChangeUrlIcon(filePath, iconPath);
                    case ".exe":
                        Log.Warning("Cannot change icon for executable files directly: {FilePath}", filePath);
                        return false;
                    default:
                        Log.Warning("Unsupported file type: {FilePath}", filePath);
                        return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to change icon for {FilePath}", filePath);
                return false;
            }
        });
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
                Log.Information("Non-ICO icon provided, may need conversion: {IconPath}", iconPath);
            }

            shortcut.IconLocation = $"{icoPath},0";
            shortcut.Save();

            Log.Information("Successfully changed icon for shortcut: {ShortcutPath}", shortcutPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to change shortcut icon: {ShortcutPath}", shortcutPath);
            return false;
        }
    }

    private static bool ChangeUrlIcon(string urlPath, string iconPath)
    {
        try
        {
            var content = File.ReadAllLines(urlPath).ToList();
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

            File.WriteAllLines(urlPath, content);

            Log.Information("Successfully changed icon for URL file: {UrlPath}", urlPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to change URL file icon: {UrlPath}", urlPath);
            return false;
        }
    }
}