using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Swapper;
using IWshRuntimeLibrary;
using System.IO;
using System.Text;
using System.Windows;
using File = System.IO.File;

namespace IconSwapperGui.UI.Services.Swapper;

public class ShortcutService(ILoggingService logger) : IShortcutService
{
    private const string PublicDesktopPath = @"C:\Users\Public\Desktop";
    private const string LnkExtension = ".lnk";
    private const string UrlExtension = ".url";
    private const string SteamUrlPrefix = "URL=steam://";
    private const string UrlPrefix = "URL=";
    private const string IconFilePrefix = "IconFile=";
    private readonly WshShell _shell = new();

    public IEnumerable<Shortcut> GetShortcuts(string? folderPath)
    {
        logger.LogInfo($"GetShortcuts called with folderPath: {folderPath}");

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            logger.LogWarning($"Folder path does not exist: {folderPath}");
            return Enumerable.Empty<Shortcut>();
        }

        var shortcuts = new List<Shortcut>();

        try
        {
            foreach (var file in EnumerateShortcutFiles(folderPath))
            {
                if (TryCreateShortcutModel(file, out var shortcut))
                {
                    shortcuts.Add(shortcut);
                }
            }

            logger.LogInfo($"Successfully created models for {shortcuts.Count} shortcuts");
        }
        catch (IOException ex)
        {
            logger.LogError($"IOException occurred while accessing folder: {folderPath}", ex);
            MessageBox.Show($"An error occurred while accessing {folderPath}: {ex.Message}",
                "Error Accessing Folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return shortcuts.OrderBy(x => x.Name);
    }

    private IEnumerable<string> EnumerateShortcutFiles(string folderPath)
    {
        logger.LogInfo($"Getting shortcut files from public desktop and folder: {folderPath}");

        return SafeEnumerateFiles(PublicDesktopPath, "*" + LnkExtension)
            .Concat(SafeEnumerateFiles(folderPath, "*" + LnkExtension))
            .Concat(SafeEnumerateFiles(folderPath, "*" + UrlExtension));
    }

    private IEnumerable<string> SafeEnumerateFiles(string folderPath, string searchPattern)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(folderPath, searchPattern, SearchOption.AllDirectories);
        }
        catch (IOException ex)
        {
            logger.LogWarning(
                $"Failed to enumerate files in {folderPath} with pattern {searchPattern}: {ex.Message}");
            return Array.Empty<string>();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(
                $"Unauthorized to enumerate files in {folderPath} with pattern {searchPattern}: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    private bool TryCreateShortcutModel(string filePath, out Shortcut shortcut)
    {
        shortcut = null!;

        var extension = Path.GetExtension(filePath);
        if (extension.Equals(UrlExtension, StringComparison.OrdinalIgnoreCase))
        {
            return TryCreateShortcutFromUrlFile(filePath, out shortcut);
        }

        if (extension.Equals(LnkExtension, StringComparison.OrdinalIgnoreCase))
        {
            return TryCreateShortcutFromLnkFile(filePath, out shortcut);
        }

        logger.LogWarning($"Unsupported shortcut extension {extension} for file {filePath}");
        return false;
    }

    private bool TryCreateShortcutFromLnkFile(string filePath, out Shortcut shortcut)
    {
        logger.LogInfo($"Creating shortcut model from .lnk file: {filePath}");

        shortcut = null!;

        try
        {
            var wshShortcut = (IWshShortcut)_shell.CreateShortcut(filePath);
            var defaultTargetPath = GetOriginalExePathFromLnkShortcut(filePath);
            var iconPath = GetIconPathFromShortcut(wshShortcut);

            shortcut = new Shortcut(Path.GetFileNameWithoutExtension(filePath), filePath, defaultTargetPath, iconPath);
            logger.LogInfo($"Successfully created shortcut model for: {shortcut.Name}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to create shortcut model from .lnk file: {filePath}", ex);
            MessageBox.Show($"Failed to create shortcut model from link file: {filePath}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private bool TryCreateShortcutFromUrlFile(string filePath, out Shortcut shortcut)
    {
        logger.LogInfo($"Creating shortcut model from .url file: {filePath}");

        shortcut = null!;

        try
        {
            if (!TryReadUrlShortcut(filePath, out var url, out var iconPath))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning($"No Steam URL found in .url file: {filePath}");
                return false;
            }

            shortcut = new Shortcut(Path.GetFileNameWithoutExtension(filePath), filePath, url, iconPath);
            logger.LogInfo($"Successfully created Steam shortcut model for: {shortcut.Name}");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to create shortcut model from .url file: {filePath}", ex);
            MessageBox.Show($"Failed to create shortcut model from URL file: {filePath}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private bool TryReadUrlShortcut(string urlFilePath, out string? url, out string? iconFile)
    {
        url = null;
        iconFile = null;

        using var reader = new StreamReader(urlFilePath);
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith(UrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                url = line[UrlPrefix.Length..];
            }
            else if (line.StartsWith(IconFilePrefix, StringComparison.OrdinalIgnoreCase))
            {
                iconFile = line[IconFilePrefix.Length..];
            }
            else if (line.StartsWith(SteamUrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                url = line[UrlPrefix.Length..];
            }
        }

        return !string.IsNullOrWhiteSpace(url);
    }

    private string GetIconPathFromShortcut(IWshShortcut shortcut)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(shortcut.IconLocation))
            {
                logger.LogInfo("No icon location found in shortcut, returning empty string");
                return string.Empty;
            }

            var iconLocationParts = shortcut.IconLocation.Split(',');

            if (string.IsNullOrWhiteSpace(iconLocationParts[0]))
            {
                logger.LogInfo($"Icon location file part is empty, using target path: {shortcut.TargetPath}");
                return shortcut.TargetPath;
            }

            var result = iconLocationParts.Length switch
            {
                > 1 when int.TryParse(iconLocationParts[1], out var iconIndex) && iconIndex == 0 => iconLocationParts
                    [0],
                1 => shortcut.IconLocation,
                _ => string.Empty
            };

            logger.LogInfo($"Resolved icon path: {result} from IconLocation: {shortcut.IconLocation}");

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError("Error getting icon path from shortcut", ex);
            return string.Empty;
        }
    }

    private string GetOriginalExePathFromLnkShortcut(string shortcutPath)
    {
        logger.LogInfo($"Getting original exe path from .lnk shortcut: {shortcutPath}");

        try
        {
            var wshShell = (WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            var shortcut = (IWshShortcut)wshShell.CreateShortcut(shortcutPath);

            if (!string.IsNullOrWhiteSpace(shortcut.TargetPath) && File.Exists(shortcut.TargetPath))
            {
                logger.LogInfo($"Found valid target path: {shortcut.TargetPath}");
                return shortcut.TargetPath;
            }

            logger.LogWarning($"Target path not found or invalid, trying metadata extraction for: {shortcutPath}");
            return GetExePathFromMetadata(shortcutPath);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to get original exe path from shortcut: {shortcutPath}", ex);
            MessageBox.Show($"Failed to get the original executable path from shortcut: {shortcutPath}\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return string.Empty;
        }
    }

    private string GetExePathFromMetadata(string shortcutPath)
    {
        logger.LogInfo($"Attempting to extract exe path from metadata: {shortcutPath}");

        try
        {
            using var fileStream = new FileStream(shortcutPath, FileMode.Open, FileAccess.Read);
            using var binaryReader = new BinaryReader(fileStream);

            fileStream.Seek(0x14, SeekOrigin.Begin);
            var bytes = binaryReader.ReadBytes(260);

            var originalPath = Encoding.UTF8.GetString(bytes).Trim('\0');

            if (!string.IsNullOrWhiteSpace(originalPath) && File.Exists(originalPath))
            {
                logger.LogInfo($"Successfully extracted exe path from metadata: {originalPath}");
                return originalPath;
            }

            logger.LogWarning($"Extracted path from metadata is invalid or does not exist: {originalPath}");
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to extract exe path from metadata for: {shortcutPath}", ex);
        }

        return string.Empty;
    }
}