using System.IO;
using System.Windows;
using IconSwapperGui.Core.Interfaces;
using IWshRuntimeLibrary;

namespace IconSwapperGui.UI.Services.Swapper;

public class LnkSwapperService(IElevationService elevationService) : ILnkSwapperService
{
    public void Swap(string applicationPath, string iconPath, string applicationName)
    {
        const string publicDesktopPath = @"C:\Users\Public\Desktop";

        if (Path.GetDirectoryName(applicationPath)!.Equals(publicDesktopPath) &&
            !elevationService.IsRunningAsAdministrator())
        {
            MessageBox.Show(
                $"To change the icon of {applicationName}, the application needs to be restarted with administrator permissions.\n\nYou will need to attempt the swap again afterwards",
                "Permissions Required To Swap Icon", MessageBoxButton.OK, MessageBoxImage.Information);

            elevationService.ElevateApplicationViaUac();
        }

        var wshShell = (WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
        var shortcut = (IWshShortcut)wshShell.CreateShortcut(applicationPath);
        shortcut.IconLocation = $"{iconPath},0";
        shortcut.Save();
    }
}