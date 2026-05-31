using System.IO;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace IconSwapperGui.UI.Services.Swapper;

public class LnkSwapperService(
    IElevationService elevationService,
    IShellIconRefreshService shellIconRefreshService) : ILnkSwapperService
{
    private const string PublicDesktopPath = @"C:\Users\Public\Desktop";

    public IconOperationResult Swap(string applicationPath, string iconPath, string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationPath) || !File.Exists(applicationPath))
            return IconOperationResult.Failure("Shortcut Not Found", "The selected shortcut could not be found.");

        if (!string.Equals(Path.GetExtension(applicationPath), ".lnk", StringComparison.OrdinalIgnoreCase))
            return IconOperationResult.Failure("Unsupported Shortcut", "Only .lnk shortcuts can be updated by this operation.");

        if (string.IsNullOrWhiteSpace(iconPath) || !File.Exists(iconPath))
            return IconOperationResult.Failure("Icon Not Found", "The selected replacement icon could not be found.");

        if (!string.Equals(Path.GetExtension(iconPath), ".ico", StringComparison.OrdinalIgnoreCase))
            return IconOperationResult.Failure("Unsupported Icon", "Windows shortcuts require an .ico file.");

        if (RequiresElevation(applicationPath))
        {
            elevationService.ElevateApplicationViaUac();
            return IconOperationResult.Failure(
                "Administrator Restart Required",
                $"To change the icon of {applicationName}, the application must restart with administrator permissions. Try the swap again after restart.");
        }

        try
        {
            var wshShell = (WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            var shortcut = (IWshShortcut)wshShell.CreateShortcut(applicationPath);
            shortcut.IconLocation = $"{iconPath},0";
            shortcut.Save();
            shellIconRefreshService.RefreshIcon(applicationPath);

            return IconOperationResult.Success("Icon Updated", "The shortcut icon was updated successfully.");
        }
        catch (Exception ex)
        {
            return IconOperationResult.Failure("Icon Update Failed", "Failed to update the shortcut icon.", ex.Message);
        }
    }

    public IconOperationResult RestoreDefault(string applicationPath, string applicationName)
    {
        if (string.IsNullOrWhiteSpace(applicationPath) || !File.Exists(applicationPath))
            return IconOperationResult.Failure("Shortcut Not Found", "The selected shortcut could not be found.");

        if (!string.Equals(Path.GetExtension(applicationPath), ".lnk", StringComparison.OrdinalIgnoreCase))
            return IconOperationResult.Failure("Unsupported Shortcut", "Only .lnk shortcuts can be restored by this operation.");

        if (RequiresElevation(applicationPath))
        {
            elevationService.ElevateApplicationViaUac();
            return IconOperationResult.Failure(
                "Administrator Restart Required",
                $"To restore the icon of {applicationName}, the application must restart with administrator permissions. Try the restore again after restart.");
        }

        try
        {
            var wshShell = (WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
            var shortcut = (IWshShortcut)wshShell.CreateShortcut(applicationPath);

            shortcut.IconLocation = File.Exists(shortcut.TargetPath)
                ? $"{shortcut.TargetPath},0"
                : string.Empty;
            shortcut.Save();
            shellIconRefreshService.RefreshIcon(applicationPath);

            return IconOperationResult.Success("Icon Restored", "The shortcut icon was restored to its default.");
        }
        catch (Exception ex)
        {
            return IconOperationResult.Failure("Icon Restore Failed", "Failed to restore the shortcut icon.", ex.Message);
        }
    }

    private bool RequiresElevation(string applicationPath)
        => string.Equals(Path.GetDirectoryName(applicationPath), PublicDesktopPath, StringComparison.OrdinalIgnoreCase) &&
           !elevationService.IsRunningAsAdministrator();
}
