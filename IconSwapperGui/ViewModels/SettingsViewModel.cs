using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using IconSwapperGui.Commands;
using IconSwapperGui.Services.Interfaces;
using Microsoft.Win32;
using Serilog;
using Application = System.Windows.Application;

namespace IconSwapperGui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "IconSwapperGui";
    private readonly ILogger _logger = Log.ForContext<SettingsViewModel>();

    [ObservableProperty] private bool? _isAutoUpdateEnabled;

    [ObservableProperty] private bool? _isDarkModeEnabled;

    [ObservableProperty] private bool? _isLaunchAtStartupEnabled;
    [ObservableProperty] private bool? _isSeasonalEffectsEnabled;

    public SettingsViewModel(ISettingsService? settingsService)
    {
        _logger.Information("SettingsViewModel initializing");

        SettingsService = settingsService;
        _isDarkModeEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableDarkMode");
        _isLaunchAtStartupEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableLaunchAtStartup");
        _isAutoUpdateEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableAutoUpdate");
        _isSeasonalEffectsEnabled = SettingsService?.GetSettingsFieldValue<bool>("EnableSeasonalEffects");

        _logger.Information(
            "Settings loaded - DarkMode: {DarkMode}, LaunchAtStartup: {LaunchAtStartup}, AutoUpdate: {AutoUpdate}, SeasonalEffects: {SeasonalEffects}",
            _isDarkModeEnabled, _isLaunchAtStartupEnabled, _isAutoUpdateEnabled, _isSeasonalEffectsEnabled);

        ToggleDarkModeCommand = new RelayCommand(_ => ToggleDarkMode());
        ToggleLaunchAtStartupCommand = new RelayCommand(_ => ToggleLaunchAtStartup());
        ToggleAutoUpdateCommand = new RelayCommand(_ => ToggleAutoUpdate());
        ToggleSeasonalEffectsCommand = new RelayCommand(_ => ToggleSeasonalEffects());

        ApplyTheme();
        _logger.Information("SettingsViewModel initialized successfully");
    }

    private ISettingsService? SettingsService { get; }

    public RelayCommand ToggleDarkModeCommand { get; }
    public RelayCommand ToggleLaunchAtStartupCommand { get; }
    public RelayCommand ToggleAutoUpdateCommand { get; }
    public RelayCommand ToggleSeasonalEffectsCommand { get; }

    public void ToggleDarkMode()
    {
        _logger.Information("ToggleDarkMode called, current value: {IsDarkModeEnabled}", IsDarkModeEnabled);

        if (!IsDarkModeEnabled.HasValue)
        {
            _logger.Warning("IsDarkModeEnabled has no value, cannot toggle");
            return;
        }

        try
        {
            SettingsService?.SaveEnableDarkMode(IsDarkModeEnabled.Value);
            _logger.Information("Dark mode toggled to: {IsDarkModeEnabled}", IsDarkModeEnabled.Value);

            ApplyTheme();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error toggling dark mode");
        }
    }

    public void ToggleLaunchAtStartup()
    {
        _logger.Information("ToggleLaunchAtStartup called, current value: {IsLaunchAtStartupEnabled}",
            IsLaunchAtStartupEnabled);

        if (!IsLaunchAtStartupEnabled.HasValue)
        {
            _logger.Warning("IsLaunchAtStartupEnabled has no value, cannot toggle");
            return;
        }

        try
        {
            SettingsService?.SaveEnableLaunchAtStartup(IsLaunchAtStartupEnabled.Value);
            _logger.Information("Launch at startup toggled to: {IsLaunchAtStartupEnabled}",
                IsLaunchAtStartupEnabled.Value);

            UpdateLaunchAtStartupRegistry(IsLaunchAtStartupEnabled.Value);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error toggling launch at startup");
        }
    }

    public void ToggleSeasonalEffects()
    {
        _logger.Information("ToggleSeasonalEffects called, current value: {IsSeasonalEffectsEnabled}",
            IsSeasonalEffectsEnabled);

        if (IsSeasonalEffectsEnabled.HasValue)
        {
            try
            {
                SettingsService?.SaveEnableSeasonalEffects(IsSeasonalEffectsEnabled.Value);
                _logger.Information("Seasonal effects toggled to: {IsSeasonalEffectsEnabled}",
                    IsSeasonalEffectsEnabled.Value);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error toggling seasonal effects");
            }
        }
        else
        {
            _logger.Warning("IsSeasonalEffectsEnabled has no value, cannot toggle");
        }
    }

    partial void OnIsDarkModeEnabledChanged(bool? value)
    {
        _logger.Information("IsDarkModeEnabled property changed to: {Value}", value);
        ApplyTheme();
    }

    partial void OnIsLaunchAtStartupEnabledChanged(bool? value)
    {
        _logger.Information("IsLaunchAtStartupEnabled property changed to: {Value}", value);
        ToggleLaunchAtStartup();
    }

    partial void OnIsAutoUpdateEnabledChanged(bool? value)
    {
        _logger.Information("IsAutoUpdateEnabled property changed to: {Value}", value);
    }

    partial void OnIsSeasonalEffectsEnabledChanged(bool? value)
    {
        _logger.Information("IsSeasonalEffectsEnabled property changed to: {Value}", value);
    }

    private void ToggleAutoUpdate()
    {
        _logger.Information("ToggleAutoUpdate called, current value: {IsAutoUpdateEnabled}", IsAutoUpdateEnabled);

        if (IsAutoUpdateEnabled.HasValue)
        {
            try
            {
                SettingsService?.SaveEnableAutoUpdate(IsAutoUpdateEnabled.Value);
                _logger.Information("Auto update toggled to: {IsAutoUpdateEnabled}", IsAutoUpdateEnabled.Value);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error toggling auto update");
            }
        }
        else
        {
            _logger.Warning("IsAutoUpdateEnabled has no value, cannot toggle");
        }
    }

    private void UpdateLaunchAtStartupRegistry(bool enable)
    {
        _logger.Information("Updating launch at startup registry, enable: {Enable}", enable);

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupKey, true);

            if (key == null)
            {
                _logger.Error("Unable to open registry key: {StartupKey}", StartupKey);
                return;
            }

            if (enable)
            {
                var executablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconSwapperGui.exe");
                key.SetValue(AppName, $"\"{executablePath}\"");
                _logger.Information("Added application to startup registry with path: {ExecutablePath}",
                    executablePath);
            }
            else
            {
                key.DeleteValue(AppName, false);
                _logger.Information("Removed application from startup registry");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating registry for launch at startup");
        }
    }

    private void ApplyTheme()
    {
        try
        {
            var themeUri = IsDarkModeEnabled == true
                ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
                : new Uri("pack://application:,,,/Themes/LightTheme.xaml");

            _logger.Information("Applying theme: {Theme}", IsDarkModeEnabled == true ? "Dark" : "Light");

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });

            _logger.Information("Theme applied successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error applying theme, IsDarkModeEnabled: {IsDarkModeEnabled}", IsDarkModeEnabled);
        }
    }
}