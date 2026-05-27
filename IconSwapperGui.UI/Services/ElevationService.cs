using IconSwapperGui.Core.Interfaces;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace IconSwapperGui.UI.Services;

public class ElevationService(IDialogService dialogService) : IElevationService
{
    public void ElevateApplicationViaUac()
    {
        var processInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            WorkingDirectory = Environment.CurrentDirectory,
            FileName = Process.GetCurrentProcess().MainModule?.FileName,
            Verb = "runas",
            Arguments = "elevate"
        };

        try
        {
            Process.Start(processInfo);
            Application.Current.Shutdown();
        }
        catch (Exception)
        {
            dialogService.ShowError(
                "Elevation Required",
                "This operation requires elevated permissions. Please run the application as an administrator.");
        }
    }

    public bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();

        var principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
