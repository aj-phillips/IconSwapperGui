using System.IO;
using System.Windows;
using IconSwapperGui.Commands;
using IconSwapperGui.Interfaces;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace IconSwapperGui.ViewModels;

public class SettingsViewModel : ViewModel
{
    private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "IconSwapperGui";

    private bool? _isDarkModeEnabled;

    private bool? _isLaunchAtStartupEnabled;

    public SettingsViewModel(ISettingsService settingsService)
    {
        SettingsService = settingsService;
        _isDarkModeEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableDarkMode");
        _isLaunchAtStartupEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableLaunchAtStartup");

        ToggleDarkModeCommand = new RelayCommand(param => ToggleDarkMode());
        ToggleLaunchAtStartupCommand = new RelayCommand(param => ToggleLaunchAtStartup());

        ApplyTheme();
    }

    public ISettingsService SettingsService { get; set; }

    public bool? IsDarkModeEnabled
    {
        get => _isDarkModeEnabled;
        set
        {
            if (_isDarkModeEnabled != value)
            {
                _isDarkModeEnabled = value;
                OnPropertyChanged();
                ApplyTheme();
            }
        }
    }

    public bool? IsLaunchAtStartupEnabled
    {
        get => _isLaunchAtStartupEnabled;
        set
        {
            if (_isLaunchAtStartupEnabled != value)
            {
                _isLaunchAtStartupEnabled = value;
                OnPropertyChanged();
                ToggleLaunchAtStartup();
            }
        }
    }

    public RelayCommand ToggleDarkModeCommand { get; }
    public RelayCommand ToggleLaunchAtStartupCommand { get; }

    public void ToggleDarkMode()
    {
        if (IsDarkModeEnabled.HasValue)
        {
            SettingsService.SaveEnableDarkMode(IsDarkModeEnabled.Value);
            ApplyTheme();
        }
    }

    public void ToggleLaunchAtStartup()
    {
        if (IsLaunchAtStartupEnabled.HasValue)
        {
            SettingsService.SaveEnableLaunchAtStartup(IsLaunchAtStartupEnabled.Value);
            UpdateLaunchAtStartupRegistry(IsLaunchAtStartupEnabled.Value);
        }
    }

    private void UpdateLaunchAtStartupRegistry(bool enable)
    {
        using (var key = Registry.CurrentUser.OpenSubKey(StartupKey, true))
        {
            if (key != null)
            {
                if (enable)
                {
                    var executablePath =
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

    private void ApplyTheme()
    {
        var themeUri = IsDarkModeEnabled == true
            ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
            : new Uri("pack://application:,,,/Themes/LightTheme.xaml");
        
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }
}