using System.Collections.ObjectModel;
using System.IO;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;

namespace IconSwapperGui.Services;

public class IconManagementService : IIconManagementService
{
    private readonly List<string> _supportedExtensions = new() { ".png", ".jpg", ".jpeg" };

    public IEnumerable<Icon> GetIcons(string folderPath, IEnumerable<string> extensions)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be null or empty.", nameof(folderPath));

        var enumerable = extensions.ToList();

        if (extensions == null || !enumerable.Any())
            throw new ArgumentException("Extensions cannot be null or empty.", nameof(extensions));

        var icons = new List<Icon>();

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Directory {folderPath} does not exist.");

        try
        {
            icons.AddRange(from extension in enumerable
                from file in Directory.GetFiles(folderPath, $"*.{extension.TrimStart('*', '.')}")
                select new Icon(Path.GetFileName(file), file));

            return icons;
        }
        catch (IOException ex)
        {
            throw new IOException($"An error occurred while accessing {folderPath}: {ex.Message}", ex);
        }
    }

    public ObservableCollection<Icon> GetIcons(string folderPath)
    {
        var icons = new ObservableCollection<Icon>();

        if (!string.IsNullOrEmpty(folderPath))
        {
            var iconList = GetIcons(folderPath, _supportedExtensions);

            foreach (var icon in iconList)
                if (!icons.Any(x => x.Path == icon.Path))
                    icons.Add(icon);
        }

        return icons;
    }

    public ObservableCollection<Icon> FilterIcons(ObservableCollection<Icon> icons, string filterString)
    {
        if (string.IsNullOrEmpty(filterString)) return new ObservableCollection<Icon>(icons);

        var filteredIcons = icons
            .Where(icon => icon.Name.Contains(filterString, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new ObservableCollection<Icon>(filteredIcons);
    }
}