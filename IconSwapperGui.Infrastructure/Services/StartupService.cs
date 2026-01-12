using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;
using IconSwapperGui.Core.Interfaces;

namespace IconSwapperGui.Infrastructure.Services;

public class StartupService : IStartupService
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "IconSwapperGui";

    public void EnableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null)
                return;

            var executablePath = GetExecutablePath();
            if (string.IsNullOrEmpty(executablePath))
                return;

            key.SetValue(AppName, $"\"{executablePath}\"");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to enable startup: {ex.Message}");
        }
    }

    public void DisableStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null)
                return;

            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to disable startup: {ex.Message}");
        }
    }

    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            if (key == null)
                return false;

            var value = key.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to check startup status: {ex.Message}");
            return false;
        }
    }

    private static string GetExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath))
            return processPath;

        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return assembly.Location;
    }
}
