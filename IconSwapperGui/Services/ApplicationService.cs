using IWshRuntimeLibrary;
using System.IO;
using System.Windows;
using System.Text.RegularExpressions;
using IconSwapperGui.Interfaces;
using Application = IconSwapperGui.Models.Application;

namespace IconSwapperGui.Services;

public class ApplicationService : IApplicationService
{
    private const string PublicDesktopPath = "C:\\Users\\Public\\Desktop";
    
    public IEnumerable<Application> GetApplications(string folderPath)
    {
        var applications = new List<Application>();

        try
        {
            if (!Directory.Exists(folderPath)) return applications;

            var publicShortcutFiles = Directory.GetFiles(PublicDesktopPath, "*.lnk", SearchOption.AllDirectories);
            var shortcutFiles = Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories);
            var steamShortcuts = Directory.GetFiles(folderPath, "*.url", SearchOption.AllDirectories);
            shortcutFiles = shortcutFiles.Concat(publicShortcutFiles).Concat(steamShortcuts).ToArray();

            foreach (var file in shortcutFiles)
            {
                var shell = new WshShell();

                if (Path.GetExtension(file).Equals(".url", StringComparison.CurrentCultureIgnoreCase))
                {
                    CreateApplicationFromUrlFile(file, applications);
                }
                else
                {
                    CreateApplicationFromLnkFile(shell, file, applications);
                }
            }
        }
        catch (IOException ex)
        {
            MessageBox.Show($"An error occurred while accessing {folderPath}: {ex.Message}",
                "Error Accessing Folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return applications;
    }

    private static void CreateApplicationFromLnkFile(WshShell shell, string file, List<Application> applications)
    {
        var shortcut = (IWshShortcut)shell.CreateShortcut(file);

        var iconPath = "";

        if (!string.IsNullOrWhiteSpace(shortcut.IconLocation))
        {
            var iconLocationParts = shortcut.IconLocation.Split(',');

            if (string.IsNullOrWhiteSpace(iconLocationParts[0]))
            {
                iconPath = shortcut.TargetPath;
            }
            else
            {
                iconPath = iconLocationParts.Length switch
                {
                    > 1 when int.TryParse(iconLocationParts[1], out var iconIndex) && iconIndex == 0 =>
                        iconLocationParts[0],
                    1 => shortcut.IconLocation,
                    _ => iconPath
                };
            }
        }

        var app = new Application(Path.GetFileNameWithoutExtension(file), file, iconPath);
                    
        applications.Add(app);
    }

    private static void CreateApplicationFromUrlFile(string file, List<Application> applications)
    {
        using var reader = new StreamReader(file);

        var steamId = "";
        var iconPath = "";

        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("URL=steam://"))
            {
                steamId = line.Substring(11);
            }
            else if (line.StartsWith("IconFile="))
            {
                iconPath = Regex.Match(line, @"IconFile=(.*)").Groups[1].Value;
            }
        }

        if (string.IsNullOrEmpty(steamId)) return;

        var app = new Application(Path.GetFileNameWithoutExtension(file), file, iconPath);

        applications.Add(app);
    }
}