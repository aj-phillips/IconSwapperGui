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

                string targetPath = shortcut.TargetPath;

                var app = new Application(Path.GetFileNameWithoutExtension(file), targetPath);
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