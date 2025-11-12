using System.Collections.ObjectModel;
using System.IO;
using IconSwapperGui.Models;
using IconSwapperGui.Services.Interfaces;
using Serilog;

namespace IconSwapperGui.Services;

public class IconManagementService : IIconManagementService
{
    private readonly List<string> _supportedExtensions = new() { ".png", ".jpg", ".jpeg" };
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

                foreach (var file in files)
                {
                    icons.Add(new Icon(Path.GetFileName(file), file));
                }
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
                if (icons.All(x => x.Path != icon.Path))
                {
                    icons.Add(icon);
                    addedCount++;
                }
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
}