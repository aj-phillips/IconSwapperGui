using System.IO;
using System.Windows;
using IconSwapperGui.Models;

namespace IconSwapperGui.Services;

public class IconService : IIconService
{
    public IEnumerable<Icon> GetIcons(string folderPath)
    {
        var icons = new List<Icon>();

        try
        {
            if (!Directory.Exists(folderPath)) return icons;

            string[] iconFiles = Directory.GetFiles(folderPath, "*.ico");

            foreach (var file in iconFiles)
            {
                var icon = new Icon(Path.GetFileName(file), file);
                icons.Add(icon);
            }
        }
        catch (IOException ex)
        {
            MessageBox.Show($"An error occurred while accessing {folderPath}: {ex.Message}",
                               "Error Accessing Folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return icons;
    }
}