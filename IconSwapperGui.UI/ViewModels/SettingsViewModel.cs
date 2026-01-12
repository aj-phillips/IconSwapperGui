using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Settings;

namespace IconSwapperGui.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IUpdateService _updateService;

    // Application Settings
    public ObservableCollection<string> ShortcutLocations { get; }
    public ObservableCollection<string> IconLocations { get; }
    public ObservableCollection<string> FolderShortcutLocations { get; }
    public ObservableCollection<string> ConverterIconsLocations { get; }

    [ObservableProperty] private string? _selectedShortcutLocation;
    [ObservableProperty] private string? _selectedIconLocation;
    [ObservableProperty] private string? _selectedFolderShortcutLocation;
    [ObservableProperty] private string? _selectedConverterIconsLocation;

    [ObservableProperty] private string _exportLocation = string.Empty;

    // Advanced Settings
    [ObservableProperty] private string _checkForUpdatesButtonText = "Check for Updates";
    [ObservableProperty] private bool _enableLogging;

    // General Settings
    [ObservableProperty] private bool _checkForUpdates;

    // Update check UI
    [ObservableProperty] private bool _isCheckingForUpdates;
    [ObservableProperty] private bool _isDownloadingUpdate;
    [ObservableProperty] private UpdateInfo? _availableUpdate;

    // Appearance Settings
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private bool _isLightTheme;
    [ObservableProperty] private string _lastCheckedDate = string.Empty;

    // Notification Settings
    [ObservableProperty] private bool _playSound;
    [ObservableProperty] private string _selectedSection = "Appearance";
    [ObservableProperty] private string _updateStatusMessage = "Up to date";

    partial void OnSelectedShortcutLocationChanged(string? value)
        => RemoveShortcutLocationCommand.NotifyCanExecuteChanged();

    partial void OnSelectedIconLocationChanged(string? value)
        => RemoveIconLocationCommand.NotifyCanExecuteChanged();

    partial void OnSelectedFolderShortcutLocationChanged(string? value)
        => RemoveFolderShortcutLocationCommand.NotifyCanExecuteChanged();

    partial void OnSelectedConverterIconsLocationChanged(string? value)
        => RemoveConverterIconsLocationCommand.NotifyCanExecuteChanged();

    partial void OnAvailableUpdateChanged(UpdateInfo? value)
        => InstallUpdateCommand.NotifyCanExecuteChanged();

    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        INotificationService notificationService,
        IUpdateService updateService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _notificationService = notificationService;
        _updateService = updateService;

        ShortcutLocations = new ObservableCollection<string>(_settingsService.Settings.Application.ShortcutLocations);
        IconLocations = new ObservableCollection<string>(_settingsService.Settings.Application.IconLocations);
        FolderShortcutLocations =
            new ObservableCollection<string>(_settingsService.Settings.Application.FolderShortcutLocations);
        ConverterIconsLocations =
            new ObservableCollection<string>(_settingsService.Settings.Application.ConverterIconsLocations);
        _exportLocation = _settingsService.Settings.Application.ExportLocation;

        _isLightTheme = _settingsService.Settings.Appearance.Theme == ThemeMode.Light;
        _isDarkTheme = _settingsService.Settings.Appearance.Theme == ThemeMode.Dark;
        _checkForUpdates = _settingsService.Settings.General.CheckForUpdates;

        _playSound = _settingsService.Settings.Notifications.PlaySound;

        _enableLogging = _settingsService.Settings.Advanced.EnableLogging;

        ShortcutLocations.CollectionChanged += (_, __) => _ = SyncApplicationSettingsAsync();
        IconLocations.CollectionChanged += (_, __) => _ = SyncApplicationSettingsAsync();
        FolderShortcutLocations.CollectionChanged += (_, __) => _ = SyncApplicationSettingsAsync();
        ConverterIconsLocations.CollectionChanged += (_, __) => _ = SyncApplicationSettingsAsync();

        PropertyChanged += SettingsViewModel_PropertyChanged;
    }

    private async Task SyncApplicationSettingsAsync()
    {
        _settingsService.Settings.Application.ShortcutLocations = ShortcutLocations.ToList();
        _settingsService.Settings.Application.IconLocations = IconLocations.ToList();
        _settingsService.Settings.Application.FolderShortcutLocations = FolderShortcutLocations.ToList();
        _settingsService.Settings.Application.ConverterIconsLocations = ConverterIconsLocations.ToList();
        _settingsService.Settings.Application.ExportLocation = ExportLocation;

        await _settingsService.SaveSettingsAsync();
    }

    private static string? BrowseForFolder(string? initialPath)
    {
        var dialog = new OpenFolderDialog()
        {
            Title = "Select a folder",
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
        {
            dialog.InitialDirectory = initialPath;
        }

        return dialog.ShowDialog() == true
            ? dialog.FolderName
            : null;
    }

    private static bool TryAddDistinctPath(ObservableCollection<string> target, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = path.Trim();

        if (target.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
            return false;

        target.Add(path);
        return true;
    }

    [RelayCommand]
    private void AddShortcutLocation()
    {
        var selected = BrowseForFolder(ShortcutLocations.FirstOrDefault());
        if (selected is null)
            return;

        TryAddDistinctPath(ShortcutLocations, selected);
    }

    [RelayCommand(CanExecute = nameof(CanRemoveShortcutLocation))]
    private void RemoveShortcutLocation()
    {
        if (string.IsNullOrWhiteSpace(SelectedShortcutLocation))
            return;

        ShortcutLocations.Remove(SelectedShortcutLocation);
        SelectedShortcutLocation = null;
    }

    private bool CanRemoveShortcutLocation() => !string.IsNullOrWhiteSpace(SelectedShortcutLocation);

    [RelayCommand]
    private void AddIconLocation()
    {
        var selected = BrowseForFolder(IconLocations.FirstOrDefault());
        if (selected is null)
            return;

        TryAddDistinctPath(IconLocations, selected);
    }

    [RelayCommand(CanExecute = nameof(CanRemoveIconLocation))]
    private void RemoveIconLocation()
    {
        if (string.IsNullOrWhiteSpace(SelectedIconLocation))
            return;

        IconLocations.Remove(SelectedIconLocation);
        SelectedIconLocation = null;
    }

    private bool CanRemoveIconLocation() => !string.IsNullOrWhiteSpace(SelectedIconLocation);

    [RelayCommand]
    private void AddFolderShortcutLocation()
    {
        var selected = BrowseForFolder(FolderShortcutLocations.FirstOrDefault());
        if (selected is null)
            return;

        TryAddDistinctPath(FolderShortcutLocations, selected);
    }

    [RelayCommand(CanExecute = nameof(CanRemoveFolderShortcutLocation))]
    private void RemoveFolderShortcutLocation()
    {
        if (string.IsNullOrWhiteSpace(SelectedFolderShortcutLocation))
            return;

        FolderShortcutLocations.Remove(SelectedFolderShortcutLocation);
        SelectedFolderShortcutLocation = null;
    }

    private bool CanRemoveFolderShortcutLocation() => !string.IsNullOrWhiteSpace(SelectedFolderShortcutLocation);

    [RelayCommand]
    private void AddConverterIconsLocation()
    {
        var selected = BrowseForFolder(ConverterIconsLocations.FirstOrDefault());
        if (selected is null)
            return;

        TryAddDistinctPath(ConverterIconsLocations, selected);
    }

    [RelayCommand(CanExecute = nameof(CanRemoveConverterIconsLocation))]
    private void RemoveConverterIconsLocation()
    {
        if (string.IsNullOrWhiteSpace(SelectedConverterIconsLocation))
            return;

        ConverterIconsLocations.Remove(SelectedConverterIconsLocation);
        SelectedConverterIconsLocation = null;
    }

    private bool CanRemoveConverterIconsLocation() => !string.IsNullOrWhiteSpace(SelectedConverterIconsLocation);

    [RelayCommand]
    private void BrowseExportLocation()
    {
        var selected = BrowseForFolder(ExportLocation);
        if (selected is null)
            return;

        ExportLocation = selected;
    }

    [RelayCommand]
    private async Task CheckForUpdatesNowAsync()
    {
        if (IsCheckingForUpdates)
            return;

        try
        {
            IsCheckingForUpdates = true;
            UpdateStatusMessage = "Checking for updates...";
            CheckForUpdatesButtonText = "Checking...";

            var update = await _updateService.CheckForUpdatesAsync();

            LastCheckedDate = DateTime.Now.ToString("g");

            if (update == null)
            {
                UpdateStatusMessage = "Your application is up to date.";
                AvailableUpdate = null;
                _notificationService.AddNotification(
                    "No Updates",
                    "You are running the latest version."
                );
            }
            else
            {
                AvailableUpdate = update;
                UpdateStatusMessage = $"Update available: {update.Version}";
                CheckForUpdatesButtonText = "Download & Install";

                _notificationService.AddNotification(
                    "Update Available",
                    $"Version {update.Version} is available."
                );
            }
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = "Failed to check for updates.";
            AvailableUpdate = null;
            _notificationService.AddNotification("Update Check Failed", ex.Message, NotificationType.Error);
        }
        finally
        {
            IsCheckingForUpdates = false;
            if (AvailableUpdate == null && CheckForUpdatesButtonText != "Check for Updates")
                CheckForUpdatesButtonText = "Check for Updates";
        }
    }

    [RelayCommand(CanExecute = nameof(CanInstallUpdate))]
    private async Task InstallUpdateAsync()
    {
        if (AvailableUpdate == null || IsDownloadingUpdate)
            return;

        try
        {
            IsDownloadingUpdate = true;
            UpdateStatusMessage = "Downloading update...";
            CheckForUpdatesButtonText = "Downloading...";

            var success = await _updateService.DownloadAndInstallUpdateAsync(AvailableUpdate);

            if (success)
            {
                UpdateStatusMessage = "Restarting to apply update...";
            }
            else
            {
                UpdateStatusMessage = "Failed to download update.";
                _notificationService.AddNotification(
                    "Update Failed",
                    "Failed to download and install the update.",
                    NotificationType.Error
                );
                CheckForUpdatesButtonText = "Download & Install";
            }
        }
        catch (Exception ex)
        {
            UpdateStatusMessage = "Failed to install update.";
            _notificationService.AddNotification("Update Installation Failed", ex.Message, NotificationType.Error);
            CheckForUpdatesButtonText = "Download & Install";
        }
        finally
        {
            IsDownloadingUpdate = false;
        }
    }

    private bool CanInstallUpdate() => AvailableUpdate != null && !IsDownloadingUpdate;

    private async void SettingsViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IsLightTheme))
        {
            if (IsLightTheme)
            {
                _settingsService.Settings.Appearance.Theme = ThemeMode.Light;
                _themeService.ApplyTheme(ThemeMode.Light);

                if (IsDarkTheme) IsDarkTheme = false;

                await _settingsService.SaveSettingsAsync();
            }

            return;
        }

        if (e.PropertyName == nameof(IsDarkTheme))
        {
            if (IsDarkTheme)
            {
                _settingsService.Settings.Appearance.Theme = ThemeMode.Dark;
                _themeService.ApplyTheme(ThemeMode.Dark);

                if (IsLightTheme) IsLightTheme = false;

                await _settingsService.SaveSettingsAsync();
            }

            return;
        }

        if (e.PropertyName == nameof(CheckForUpdates))
        {
            _settingsService.Settings.General.CheckForUpdates = CheckForUpdates;

            await _settingsService.SaveSettingsAsync();

            return;
        }

        if (e.PropertyName == nameof(PlaySound))
        {
            _settingsService.Settings.Notifications.PlaySound = PlaySound;

            await _settingsService.SaveSettingsAsync();

            return;
        }

        if (e.PropertyName == nameof(EnableLogging))
        {
            _settingsService.Settings.Advanced.EnableLogging = EnableLogging;

            await _settingsService.SaveSettingsAsync();

            return;
        }

        if (e.PropertyName == nameof(ExportLocation))
        {
            _settingsService.Settings.Application.ExportLocation = ExportLocation;

            await _settingsService.SaveSettingsAsync();
        }
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        await _settingsService.ResetToDefaultsAsync();

        IsLightTheme = _settingsService.Settings.Appearance.Theme == ThemeMode.Light;
        IsDarkTheme = _settingsService.Settings.Appearance.Theme == ThemeMode.Dark;
        CheckForUpdates = _settingsService.Settings.General.CheckForUpdates;

        PlaySound = _settingsService.Settings.Notifications.PlaySound;

        EnableLogging = _settingsService.Settings.Advanced.EnableLogging;


        _themeService.ApplyTheme(ThemeMode.Light);

        _notificationService.AddNotification(
            "Settings Reset",
            "All settings have been reset to defaults",
            NotificationType.Success
        );
    }

    [RelayCommand]
    private void ExportSettings()
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                FileName = $"settings_export_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                DefaultExt = ".json",
                Title = "Export Settings"
            };

            if (dialog.ShowDialog() == true)
            {
                var settingsPath = _settingsService.GetSettingsFilePath();

                File.Copy(settingsPath, dialog.FileName, true);

                _notificationService.AddNotification(
                    "Settings Exported",
                    $"Settings exported to {Path.GetFileName(dialog.FileName)}",
                    NotificationType.Success
                );
            }
        }
        catch (Exception ex)
        {
            _notificationService.AddNotification(
                "Export Failed",
                $"Failed to export settings: {ex.Message}",
                NotificationType.Error
            );
        }
    }

    [RelayCommand]
    private void SelectSection(string? section)
    {
        SelectedSection = section ?? "Appearance";
    }
}