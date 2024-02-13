using IconSwapperGui.Models;
using IWshRuntimeLibrary;
using System.IO;
using System.Windows;
using Application = IconSwapperGui.Models.Application;

namespace IconSwapperGui.Services;

public class ApplicationService : IApplicationService
{
    public IEnumerable<Application> GetApplications(string folderPath)
    {
        var applications = new List<Application>();

        try
        {
            if (!Directory.Exists(folderPath)) return applications;

            string[] shortcutFiles = Directory.GetFiles(folderPath, "*.lnk");
            foreach (var file in shortcutFiles)
            {
                WshShell shell = new WshShell();
                IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(file);

                string iconPath = "";

                if (!string.IsNullOrWhiteSpace(shortcut.IconLocation))
                {
                    var iconLocationParts = shortcut.IconLocation.Split(',');

                    if (string.IsNullOrWhiteSpace(iconLocationParts[0]))
                    {
                        iconPath = shortcut.TargetPath;
                    }
                    else if (iconLocationParts.Length > 1 && int.TryParse(iconLocationParts[1], out var iconIndex) &&
                             iconIndex == 0)
                    {
                        iconPath = iconLocationParts[0];
                    }
                    else if (iconLocationParts.Length == 1)
                    {
                        iconPath = shortcut.IconLocation;
                    }
                }

                var app = new Application(Path.GetFileNameWithoutExtension(file), file, iconPath);

                applications.Add(app);
            }
        }
        catch (IOException ex)
        {
            MessageBox.Show($"An error occurred while accessing {folderPath}: {ex.Message}",
                "Error Accessing Folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return applications;
    }
}