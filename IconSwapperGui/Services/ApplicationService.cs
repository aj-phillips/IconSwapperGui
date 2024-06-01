using IconSwapperGui.Models;
using IWshRuntimeLibrary;
using System.IO;
using System.Windows;
using System.Text.RegularExpressions;
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

            string[] shortcutFiles = Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories);
            string[] steamShortcuts = Directory.GetFiles(folderPath, "*.url", SearchOption.AllDirectories);
            shortcutFiles = shortcutFiles.Concat(steamShortcuts).ToArray();

            foreach (var file in shortcutFiles)
            {
                WshShell shell = new WshShell();

                if (Path.GetExtension(file).ToLower() == ".url")
                {
                    using StreamReader reader = new StreamReader(file);
                    string line = "";
                    string steamId = "";
                    string iconPath = "";

                    while ((line = reader.ReadLine()) != null)
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

                    if (string.IsNullOrEmpty(steamId)) continue;

                    var app = new Application(Path.GetFileNameWithoutExtension(file), file, iconPath);

                    applications.Add(app);
                }
                else
                {
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(file);

                    string iconPath = "";

                    if (!string.IsNullOrWhiteSpace(shortcut.IconLocation))
                    {
                        var iconLocationParts = shortcut.IconLocation.Split(',');

                        if (string.IsNullOrWhiteSpace(iconLocationParts[0]))
                        {
                            iconPath = shortcut.TargetPath;
                        }
                        else
                            iconPath = iconLocationParts.Length switch
                            {
                                > 1 when int.TryParse(iconLocationParts[1], out var iconIndex) && iconIndex == 0 =>
                                    iconLocationParts[0],
                                1 => shortcut.IconLocation,
                                _ => iconPath
                            };
                    }

                    var app = new Application(Path.GetFileNameWithoutExtension(file), file, iconPath);
                    applications.Add(app);
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
}