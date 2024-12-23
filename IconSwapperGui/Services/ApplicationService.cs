using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using IconSwapperGui.Services.Interfaces;
using IWshRuntimeLibrary;
using ApplicationModel = IconSwapperGui.Models.Application;
using File = System.IO.File;

namespace IconSwapperGui.Services;

public class ApplicationService : IApplicationService
{
    private const string PublicDesktopPath = @"C:\Users\Public\Desktop";
    private readonly WshShell _shell = new();

    public IEnumerable<ApplicationModel> GetApplications(string? folderPath)
    {
        var applications = new List<ApplicationModel>();

        try
        {
            if (!Directory.Exists(folderPath)) return applications;

            var allShortcuts = GetShortcutFiles(folderPath);
            foreach (var file in allShortcuts)
                if (Path.GetExtension(file).Equals(".url", StringComparison.CurrentCultureIgnoreCase))
                    CreateApplicationFromUrlFile(file, applications);
                else
                    CreateApplicationFromLnkFile(file, applications);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"An error occurred while accessing {folderPath}: {ex.Message}",
                "Error Accessing Folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return applications.OrderBy(x => x.Name);
    }

    private static IEnumerable<string> GetShortcutFiles(string? folderPath)
    {
        var publicShortcutFiles = Directory.GetFiles(PublicDesktopPath, "*.lnk", SearchOption.AllDirectories);

        if (folderPath == null) return publicShortcutFiles;

        var userShortcutFiles = Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories);
        var steamShortcutFiles = Directory.GetFiles(folderPath, "*.url", SearchOption.AllDirectories);

        return userShortcutFiles.Concat(publicShortcutFiles).Concat(steamShortcutFiles);
    }

    private void CreateApplicationFromLnkFile(string file, List<ApplicationModel> applications)
    {
        try
        {
            var shortcut = (IWshShortcut)_shell.CreateShortcut(file);
            var defaultIconPath = GetOriginalExePathFromLnkShortcut(file);
            var iconPath = GetIconPathFromShortcut(shortcut);
            var app = new ApplicationModel(Path.GetFileNameWithoutExtension(file), file, defaultIconPath, iconPath);

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
            var defaultIconPath = GetOriginalExePathFromUrlShortcut(file);

            while (reader.ReadLine() is { } line)
                if (line.StartsWith("URL=steam://"))
                    steamId = line.Substring(11);
                else if (line.StartsWith("IconFile=")) iconPath = Regex.Match(line, @"IconFile=(.*)").Groups[1].Value;

            if (string.IsNullOrEmpty(steamId)) return;

            var app = new ApplicationModel(Path.GetFileNameWithoutExtension(file), file, defaultIconPath, iconPath);
            applications.Add(app);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to create application from URL file: {file}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string GetIconPathFromShortcut(IWshShortcut shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut.IconLocation)) return string.Empty;

        var iconLocationParts = shortcut.IconLocation.Split(',');

        if (string.IsNullOrWhiteSpace(iconLocationParts[0])) return shortcut.TargetPath;

        return iconLocationParts.Length switch
        {
            > 1 when int.TryParse(iconLocationParts[1], out var iconIndex) && iconIndex == 0 => iconLocationParts
                [0],
            1 => shortcut.IconLocation,
            _ => string.Empty
        };
    }

    private static string GetOriginalExePathFromLnkShortcut(string shortcutPath)
    {
        try
        {
            var wshShell = (WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            var shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);

            if (!string.IsNullOrWhiteSpace(shortcut.TargetPath) && File.Exists(shortcut.TargetPath))
                return shortcut.TargetPath;

            return GetExePathFromMetadata(shortcutPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to get the original executable path from shortcut: {shortcutPath}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return string.Empty;
        }
    }

    private static string GetOriginalExePathFromUrlShortcut(string urlFilePath)
    {
        try
        {
            using var reader = new StreamReader(urlFilePath);
            while (reader.ReadLine() is { } line)
                if (line.StartsWith("URL="))
                    return line.Substring(4);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to get the original URL path from URL file: {urlFilePath}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return string.Empty;
    }

    private static string GetExePathFromMetadata(string shortcutPath)
    {
        try
        {
            using var fileStream = new FileStream(shortcutPath, FileMode.Open, FileAccess.Read);
            using var binaryReader = new BinaryReader(fileStream);

            fileStream.Seek(0x14, SeekOrigin.Begin);
            var bytes = binaryReader.ReadBytes(260);

            var originalPath = Encoding.UTF8.GetString(bytes).Trim('\0');

            if (!string.IsNullOrWhiteSpace(originalPath) && File.Exists(originalPath)) return originalPath;
        }
        catch
        {
            // Swallow errors during fallback
        }

        return string.Empty;
    }
}