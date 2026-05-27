using System.IO;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.UI.Services.Swapper;

public class UrlSwapperService : IUrlSwapperService
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

            File.WriteAllLines(applicationPath, urlFileContent);
            return IconOperationResult.Success("Icon Updated", "The URL shortcut icon was updated successfully.");
        }
        catch (Exception ex)
        {
            return IconOperationResult.Failure("Icon Update Failed", "Failed to update the URL shortcut icon.", ex.Message);
        }
    }
}
