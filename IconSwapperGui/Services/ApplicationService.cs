using IWshRuntimeLibrary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using ApplicationModel = IconSwapperGui.Models.Application;

namespace IconSwapperGui.Services
{
    public class ApplicationService : IApplicationService
    {
        private const string PublicDesktopPath = @"C:\Users\Public\Desktop";
        private readonly WshShell _shell;

        public ApplicationService()
        {
            _shell = new WshShell();
        }

        public IEnumerable<ApplicationModel> GetApplications(string folderPath)
        {
            var applications = new List<ApplicationModel>();

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    return applications;
                }

                var allShortcuts = GetShortcutFiles(folderPath);
                foreach (var file in allShortcuts)
                {
                    if (Path.GetExtension(file).Equals(".url", StringComparison.CurrentCultureIgnoreCase))
                    {
                        CreateApplicationFromUrlFile(file, applications);
                    }
                    else
                    {
                        CreateApplicationFromLnkFile(file, applications);
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

        private IEnumerable<string> GetShortcutFiles(string folderPath)
        {
            var publicShortcutFiles = Directory.GetFiles(PublicDesktopPath, "*.lnk", SearchOption.AllDirectories);
            var userShortcutFiles = Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories);
            var steamShortcutFiles = Directory.GetFiles(folderPath, "*.url", SearchOption.AllDirectories);

            return userShortcutFiles.Concat(publicShortcutFiles).Concat(steamShortcutFiles);
        }

        private void CreateApplicationFromLnkFile(string file, List<ApplicationModel> applications)
        {
            try
            {
                var shortcut = (IWshShortcut)_shell.CreateShortcut(file);
                var iconPath = GetIconPathFromShortcut(shortcut);
                var app = new ApplicationModel(Path.GetFileNameWithoutExtension(file), file, iconPath);

                applications.Add(app);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create application from link file: {file}\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void CreateApplicationFromUrlFile(string file, List<ApplicationModel> applications)
        {
            try
            {
                using var reader = new StreamReader(file);
                var steamId = string.Empty;
                var iconPath = string.Empty;

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

                var app = new ApplicationModel(Path.GetFileNameWithoutExtension(file), file, iconPath);
                applications.Add(app);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create application from URL file: {file}\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetIconPathFromShortcut(IWshShortcut shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut.IconLocation))
            {
                return string.Empty;
            }

            var iconLocationParts = shortcut.IconLocation.Split(',');

            if (string.IsNullOrWhiteSpace(iconLocationParts[0]))
            {
                return shortcut.TargetPath;
            }

            return iconLocationParts.Length switch
            {
                > 1 when int.TryParse(iconLocationParts[1], out var iconIndex) && iconIndex == 0 => iconLocationParts
                    [0],
                1 => shortcut.IconLocation,
                _ => string.Empty
            };
        }
    }
}