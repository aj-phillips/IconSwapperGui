using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using IconSwapperGui.Services.Interfaces;
using IWshRuntimeLibrary;
using Serilog;
using ApplicationModel = IconSwapperGui.Models.Application;
using File = System.IO.File;

namespace IconSwapperGui.Services;

public class ApplicationService : IApplicationService
{
    private const string PublicDesktopPath = @"C:\Users\Public\Desktop";
    private readonly WshShell _shell = new();
    private readonly ILogger _logger = Log.ForContext<ApplicationService>();

    public IEnumerable<ApplicationModel> GetApplications(string? folderPath)
    {
        _logger.Information("GetApplications called with folderPath: {FolderPath}", folderPath ?? "null");
        var applications = new List<ApplicationModel>();

        try
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.Warning("Folder path does not exist: {FolderPath}", folderPath ?? "null");
                return applications;
            }

            var allShortcuts = GetShortcutFiles(folderPath);
            _logger.Information("Found {Count} shortcut files to process", allShortcuts.Count());

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

            _logger.Information("Successfully created models for {Count} applications", applications.Count);
        }
        catch (IOException ex)
        {
            _logger.Error(ex, "IOException occurred while accessing folder: {FolderPath}", folderPath);

            MessageBox.Show($"An error occurred while accessing {folderPath}: {ex.Message}",
                "Error Accessing Folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error in GetApplications for folder: {FolderPath}", folderPath);
            throw;
        }

        return applications.OrderBy(x => x.Name);
    }

    private static IEnumerable<string> GetShortcutFiles(string? folderPath)
    {
        var logger = Log.ForContext<ApplicationService>();
        logger.Information("Getting shortcut files from public desktop and folder: {FolderPath}", folderPath ?? "null");

        try
        {
            var publicShortcutFiles = Directory.GetFiles(PublicDesktopPath, "*.lnk", SearchOption.AllDirectories);
            logger.Information("Found {Count} .lnk files in public desktop", publicShortcutFiles.Length);

            if (folderPath == null)
            {
                return publicShortcutFiles;
            }

            var userShortcutFiles = Directory.GetFiles(folderPath, "*.lnk", SearchOption.AllDirectories);
            var steamShortcutFiles = Directory.GetFiles(folderPath, "*.url", SearchOption.AllDirectories);

            logger.Information("Found {LnkCount} .lnk and {UrlCount} .url files in user folder",
                userShortcutFiles.Length, steamShortcutFiles.Length);

            return userShortcutFiles.Concat(publicShortcutFiles).Concat(steamShortcutFiles);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting shortcut files from folder: {FolderPath}", folderPath);
            throw;
        }
    }

    private void CreateApplicationFromLnkFile(string file, List<ApplicationModel> applications)
    {
        _logger.Information("Creating application from .lnk file: {FilePath}", file);

        try
        {
            var shortcut = (IWshShortcut)_shell.CreateShortcut(file);
            var defaultIconPath = GetOriginalExePathFromLnkShortcut(file);
            var iconPath = GetIconPathFromShortcut(shortcut);
            var app = new ApplicationModel(Path.GetFileNameWithoutExtension(file), file, defaultIconPath, iconPath);

            applications.Add(app);
            _logger.Information("Successfully created application model for: {FileName}",
                Path.GetFileNameWithoutExtension(file));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create application from .lnk file: {FilePath}", file);
            MessageBox.Show($"Failed to create application from link file: {file}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void CreateApplicationFromUrlFile(string file, List<ApplicationModel> applications)
    {
        var logger = Log.ForContext<ApplicationService>();
        logger.Information("Creating application from .url file: {FilePath}", file);

        try
        {
            using var reader = new StreamReader(file);
            var steamId = string.Empty;
            var iconPath = string.Empty;
            var defaultIconPath = GetOriginalExePathFromUrlShortcut(file);

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

            if (string.IsNullOrEmpty(steamId))
            {
                logger.Warning("No Steam ID found in .url file: {FilePath}", file);
                return;
            }

            var app = new ApplicationModel(Path.GetFileNameWithoutExtension(file), file, defaultIconPath, iconPath);
            applications.Add(app);
            logger.Information("Successfully created application model for Steam shortcut: {FileName}",
                Path.GetFileNameWithoutExtension(file));
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to create application from .url file: {FilePath}", file);
            MessageBox.Show($"Failed to create application from URL file: {file}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string GetIconPathFromShortcut(IWshShortcut shortcut)
    {
        var logger = Log.ForContext<ApplicationService>();

        try
        {
            if (string.IsNullOrWhiteSpace(shortcut.IconLocation))
            {
                logger.Information("No icon location found in shortcut, returning empty string");
                return string.Empty;
            }

            var iconLocationParts = shortcut.IconLocation.Split(',');

            if (string.IsNullOrWhiteSpace(iconLocationParts[0]))
            {
                logger.Information("Icon location part[0] is empty, using target path: {TargetPath}",
                    shortcut.TargetPath);
                return shortcut.TargetPath;
            }

            var result = iconLocationParts.Length switch
            {
                > 1 when int.TryParse(iconLocationParts[1], out var iconIndex) && iconIndex == 0 => iconLocationParts
                    [0],
                1 => shortcut.IconLocation,
                _ => string.Empty
            };

            logger.Information("Resolved icon path: {IconPath} from IconLocation: {IconLocation}",
                result, shortcut.IconLocation);

            return result;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Error getting icon path from shortcut");
            return string.Empty;
        }
    }

    private static string GetOriginalExePathFromLnkShortcut(string shortcutPath)
    {
        var logger = Log.ForContext<ApplicationService>();
        logger.Information("Getting original exe path from .lnk shortcut: {ShortcutPath}", shortcutPath);

        try
        {
            var wshShell = (WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            var shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);

            if (!string.IsNullOrWhiteSpace(shortcut.TargetPath) && File.Exists(shortcut.TargetPath))
            {
                logger.Information("Found valid target path: {TargetPath}", shortcut.TargetPath);
                return shortcut.TargetPath;
            }

            logger.Warning("Target path not found or invalid, trying metadata extraction for: {ShortcutPath}",
                shortcutPath);
            return GetExePathFromMetadata(shortcutPath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get original exe path from shortcut: {ShortcutPath}", shortcutPath);
            MessageBox.Show($"Failed to get the original executable path from shortcut: {shortcutPath}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return string.Empty;
        }
    }

    private static string GetOriginalExePathFromUrlShortcut(string urlFilePath)
    {
        var logger = Log.ForContext<ApplicationService>();
        logger.Information("Getting original URL path from .url file: {UrlFilePath}", urlFilePath);

        try
        {
            using var reader = new StreamReader(urlFilePath);
            while (reader.ReadLine() is { } line)
            {
                if (line.StartsWith("URL="))
                {
                    var url = line.Substring(4);
                    logger.Information("Found URL in .url file: {Url}", url);
                    return url;
                }
            }

            logger.Warning("No URL= line found in .url file: {UrlFilePath}", urlFilePath);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to get original URL path from .url file: {UrlFilePath}", urlFilePath);
            MessageBox.Show($"Failed to get the original URL path from URL file: {urlFilePath}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return string.Empty;
    }

    private static string GetExePathFromMetadata(string shortcutPath)
    {
        var logger = Log.ForContext<ApplicationService>();
        logger.Information("Attempting to extract exe path from metadata: {ShortcutPath}", shortcutPath);

        try
        {
            using var fileStream = new FileStream(shortcutPath, FileMode.Open, FileAccess.Read);
            using var binaryReader = new BinaryReader(fileStream);

            fileStream.Seek(0x14, SeekOrigin.Begin);
            var bytes = binaryReader.ReadBytes(260);

            var originalPath = Encoding.UTF8.GetString(bytes).Trim('\0');

            if (!string.IsNullOrWhiteSpace(originalPath) && File.Exists(originalPath))
            {
                logger.Information("Successfully extracted exe path from metadata: {OriginalPath}", originalPath);
                return originalPath;
            }

            logger.Warning("Extracted path from metadata is invalid or does not exist: {OriginalPath}", originalPath);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to extract exe path from metadata for: {ShortcutPath}", shortcutPath);
        }

        return string.Empty;
    }
}