using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using IconSwapperGui.Commands;
using IconSwapperGui.Services.Interfaces;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace IconSwapperGui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "IconSwapperGui";

    [ObservableProperty] private bool? _isAutoUpdateEnabled;

    [ObservableProperty] private bool? _isDarkModeEnabled;

    [ObservableProperty] private bool? _isLaunchAtStartupEnabled;

    public SettingsViewModel(ISettingsService? settingsService)
    {
        SettingsService = settingsService;
        _isDarkModeEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableDarkMode");
        _isLaunchAtStartupEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableLaunchAtStartup");
        _isAutoUpdateEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableAutoUpdate");

        ToggleDarkModeCommand = new RelayCommand(_ => ToggleDarkMode());
        ToggleLaunchAtStartupCommand = new RelayCommand(_ => ToggleLaunchAtStartup());
        ToggleAutoUpdateCommand = new RelayCommand(_ => ToggleAutoUpdate());

        ApplyTheme();
    }

    private ISettingsService? SettingsService { get; }

    public RelayCommand ToggleDarkModeCommand { get; }
    public RelayCommand ToggleLaunchAtStartupCommand { get; }
    public RelayCommand ToggleAutoUpdateCommand { get; }

    partial void OnIsDarkModeEnabledChanged(bool? value)
    {
        ApplyTheme();
    }

    partial void OnIsLaunchAtStartupEnabledChanged(bool? value)
    {
        ToggleLaunchAtStartup();
    }

    public void ToggleDarkMode()
    {
        if (!IsDarkModeEnabled.HasValue) return;

        SettingsService?.SaveEnableDarkMode(IsDarkModeEnabled.Value);

        ApplyTheme();
    }

    public void ToggleLaunchAtStartup()
    {
        if (!IsLaunchAtStartupEnabled.HasValue) return;

        SettingsService?.SaveEnableLaunchAtStartup(IsLaunchAtStartupEnabled.Value);

        UpdateLaunchAtStartupRegistry(IsLaunchAtStartupEnabled.Value);
    }

    private void ToggleAutoUpdate()
    {
        if (IsAutoUpdateEnabled.HasValue) SettingsService?.SaveEnableAutoUpdate(IsAutoUpdateEnabled.Value);
    }

    private void UpdateLaunchAtStartupRegistry(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);

        if (key == null) return;

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

    private void ApplyTheme()
    {
        var themeUri = IsDarkModeEnabled == true
            ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
            : new Uri("pack://application:,,,/Themes/LightTheme.xaml");

        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }
}