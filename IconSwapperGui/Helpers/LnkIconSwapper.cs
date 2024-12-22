using System.IO;
using IconSwapperGui.Services.Interfaces;
using IWshRuntimeLibrary;

namespace IconSwapperGui.Helpers;

public class LnkIconSwapper
{
    private readonly IDialogService _dialogService;
    private readonly IElevationService _elevationService;

    public LnkIconSwapper(IDialogService dialogService, IElevationService elevationService)
    {
        _dialogService = dialogService;
        _elevationService = elevationService;
    }

    public void Swap(string applicationPath, string iconPath, string applicationName)
    {
        const string publicDesktopPath = @"C:\Users\Public\Desktop";

        if (Path.GetDirectoryName(applicationPath)!.Equals(publicDesktopPath) &&
            !_elevationService.IsRunningAsAdministrator())
        {
            _dialogService.ShowInformation(
                $"To change the icon of {applicationName}, the application needs to be restarted with administrator permissions.\n\nYou will need to attempt the swap again afterwards",
                "Permissions Required To Swap Icon");

            _elevationService.ElevateApplicationViaUac();
        }

        var wshShell = (WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!)!;
        var shortcut = (IWshShortcut)wshShell.CreateShortcut(applicationPath);
        shortcut.IconLocation = $"{iconPath},0";
        shortcut.Save();
    }
}