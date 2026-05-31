using System.IO;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.UI.Services.Swapper;

public class UrlSwapperService(IShellIconRefreshService shellIconRefreshService) : IUrlSwapperService
{
    public IconOperationResult Swap(string applicationPath, string iconPath)
    {
        if (string.IsNullOrWhiteSpace(applicationPath) || !File.Exists(applicationPath))
            return IconOperationResult.Failure("Shortcut Not Found", "The selected URL shortcut could not be found.");

        if (!string.Equals(Path.GetExtension(applicationPath), ".url", StringComparison.OrdinalIgnoreCase))
            return IconOperationResult.Failure("Unsupported Shortcut", "Only .url shortcuts can be updated by this operation.");

        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
            return IconOperationResult.Failure("Icon Not Found", "The selected replacement icon could not be found.");

        if (!string.Equals(Path.GetExtension(iconPath), ".ico", StringComparison.OrdinalIgnoreCase))
            return IconOperationResult.Failure("Unsupported Icon", "Windows shortcuts require an .ico file.");

        try
        {
            var urlFileContent = File.ReadAllLines(applicationPath).ToList();
            var replaced = false;

            for (var i = 0; i < urlFileContent.Count; i++)
            {
                if (!urlFileContent[i].StartsWith("IconFile", StringComparison.OrdinalIgnoreCase))
                    continue;

                urlFileContent[i] = "IconFile=" + iconPath;
                replaced = true;
            }

            if (!replaced)
                urlFileContent.Add("IconFile=" + iconPath);

            EnsureIconIndex(urlFileContent);

            File.WriteAllLines(applicationPath, urlFileContent);
            shellIconRefreshService.RefreshIcon(applicationPath);
            return IconOperationResult.Success("Icon Updated", "The URL shortcut icon was updated successfully.");
        }
        catch (Exception ex)
        {
            return IconOperationResult.Failure("Icon Update Failed", "Failed to update the URL shortcut icon.", ex.Message);
        }
    }

    public IconOperationResult RestoreDefault(string applicationPath, string? originalIconPath = null)
    {
        if (string.IsNullOrWhiteSpace(applicationPath) || !File.Exists(applicationPath))
            return IconOperationResult.Failure("Shortcut Not Found", "The selected URL shortcut could not be found.");

        if (!string.Equals(Path.GetExtension(applicationPath), ".url", StringComparison.OrdinalIgnoreCase))
            return IconOperationResult.Failure("Unsupported Shortcut", "Only .url shortcuts can be restored by this operation.");

        try
        {
            var urlFileContent = File.ReadAllLines(applicationPath).ToList();

            if (!string.IsNullOrWhiteSpace(originalIconPath) && File.Exists(originalIconPath))
            {
                SetIconMetadata(urlFileContent, originalIconPath);
            }
            else
            {
                urlFileContent = urlFileContent
                    .Where(line => !line.StartsWith("IconFile", StringComparison.OrdinalIgnoreCase)
                                   && !line.StartsWith("IconIndex", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            File.WriteAllLines(applicationPath, urlFileContent);
            shellIconRefreshService.RefreshIcon(applicationPath);
            return IconOperationResult.Success("Icon Restored", "The URL shortcut icon was restored to its default.");
        }
        catch (Exception ex)
        {
            return IconOperationResult.Failure("Icon Restore Failed", "Failed to restore the URL shortcut icon.", ex.Message);
        }
    }

    private static void SetIconMetadata(IList<string> urlFileContent, string iconPath)
    {
        var replacedIconFile = false;

        for (var i = 0; i < urlFileContent.Count; i++)
        {
            if (!urlFileContent[i].StartsWith("IconFile", StringComparison.OrdinalIgnoreCase))
                continue;

            urlFileContent[i] = "IconFile=" + iconPath;
            replacedIconFile = true;
        }

        if (!replacedIconFile)
            urlFileContent.Add("IconFile=" + iconPath);

        EnsureIconIndex(urlFileContent);
    }

    private static void EnsureIconIndex(IList<string> urlFileContent)
    {
        var replacedIconIndex = false;

        for (var i = 0; i < urlFileContent.Count; i++)
        {
            if (!urlFileContent[i].StartsWith("IconIndex", StringComparison.OrdinalIgnoreCase))
                continue;

            urlFileContent[i] = "IconIndex=0";
            replacedIconIndex = true;
        }

        if (!replacedIconIndex)
            urlFileContent.Add("IconIndex=0");
    }
}
