using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Services;

namespace IconSwapperGui
{
    public partial class MainWindow : Window
    {
        private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "IconSwapperGui";
        private readonly ISettingsService _settingsService;

        private readonly string _currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public MainWindow()
        {
            InitializeComponent();

            this.Title = $"Icon Swapper - v{_currentVersion}";

            _settingsService = new SettingsService();
            RegisterInStartup();

            var args = Environment.GetCommandLineArgs();
            if (!Debugger.IsAttached && !args.Contains("--updated"))
            {
                CheckForUpdates();
            }
        }

        public void CheckForUpdates()
        {
            var updaterExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconSwapperGui.Updater.exe");

            if (File.Exists(updaterExePath))
            {
                var updaterProcess = Process.Start(updaterExePath, _currentVersion);
                if (updaterProcess != null)
                {
                    updaterProcess.WaitForExit();
                    if (updaterProcess.ExitCode == 0)
                    {
                        var newExecutablePath =
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconSwapperGui.exe");
                        Process.Start(newExecutablePath, "--updated");
                        Application.Current.Shutdown();
                    }
                }
            }
            else
            {
                MessageBox.Show("Updater could not be found. The application will start without updating.",
                    "Updater Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RegisterInStartup()
        {
            var enableStartup = _settingsService?.GetSettingsFieldValue<bool>("EnableLaunchAtStartup") ?? false;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true))
            {
                if (key != null)
                {
                    if (enableStartup)
                    {
                        string executablePath =
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconSwapperGui.exe");
                        key.SetValue(AppName, $"\"{executablePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
        }
    }
}