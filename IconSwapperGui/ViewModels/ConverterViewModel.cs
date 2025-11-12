using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.Converter;
using IconSwapperGui.Commands.Swapper;
using IconSwapperGui.Models;
using IconSwapperGui.Services;
using IconSwapperGui.Services.Interfaces;
using IconSwapperGui.ViewModels.Interfaces;
using Serilog;

namespace IconSwapperGui.ViewModels;

public partial class ConverterViewModel : ObservableObject, IIconViewModel
{
    private readonly IIconManagementService _iconManagementService;
    private readonly ILogger _logger = Log.ForContext<ConverterViewModel>();

    [ObservableProperty] private string? _applicationsLocationPath;

    [ObservableProperty] private bool _canConvertImages;

    [ObservableProperty] private ObservableCollection<Icon> _filteredIcons;

    private FileSystemWatcherService? _fsWatcherService;

    [ObservableProperty] private ObservableCollection<Icon> _icons;

    [ObservableProperty] private string? _iconsFolderPath;

    public ConverterViewModel(
        IIconManagementService iconService,
        ISettingsService settingsService,
        IDialogService dialogService)
    {
        _logger.Information("ConverterViewModel initializing");

        _iconManagementService = iconService ?? throw new ArgumentNullException(nameof(iconService));
        SettingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Icons = [];
        FilteredIcons = [];

        ConvertIconCommand = new ConvertIconCommand(this, null!, _ => true);
        ChooseIconFolderCommand = new ChooseIconFolderCommand<ConverterViewModel>(this, null!, _ => true);

        IconsFolderPath = SettingsService.GetConverterIconsLocation();
        ApplicationsLocationPath = SettingsService.GetApplicationsLocation();

        _logger.Information(
            "ConverterViewModel initialized with IconsFolderPath: {IconsFolderPath}, ApplicationsLocationPath: {ApplicationsLocationPath}",
            IconsFolderPath ?? "null", ApplicationsLocationPath ?? "null");

        LoadPreviousIcons();
    }

    public IDialogService DialogService { get; set; }
    public RelayCommand ConvertIconCommand { get; }
    public RelayCommand ChooseIconFolderCommand { get; }

    public bool CanDeleteImagesAfterConversion { get; set; }

    public string? FilterString { get; set; }

    public ISettingsService SettingsService { get; set; }

    public void PopulateIconsList(string? folderPath)
    {
        _logger.Information("PopulateIconsList called with folderPath: {FolderPath}", folderPath ?? "null");

        if (string.IsNullOrEmpty(folderPath))
        {
            _logger.Warning("Folder path is null or empty, cannot populate icons list");
            return;
        }

        if (!Directory.Exists(folderPath))
        {
            _logger.Warning("Folder path does not exist: {FolderPath}", folderPath);
            return;
        }

        try
        {
            Icons = _iconManagementService.GetIcons(folderPath);
            _logger.Information("Populated icons list with {Count} icons", Icons.Count);

            FilterIcons();
            UpdateConvertButtonEnabledState();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating icons list from folder: {FolderPath}", folderPath);
        }
    }

    partial void OnIconsFolderPathChanged(string? value)
    {
        _logger.Information("IconsFolderPath changed to: {IconsFolderPath}", value ?? "null");
        ValidateAndSetupFileSystemWatcher();
    }

    partial void OnIconsChanged(ObservableCollection<Icon> value)
    {
        _logger.Information("Icons collection changed, new count: {Count}", value?.Count ?? 0);
        FilterIcons();
    }

    private void ValidateAndSetupFileSystemWatcher()
    {
        _logger.Information("Validating and setting up file system watcher for: {IconsFolderPath}",
            IconsFolderPath ?? "null");

        _fsWatcherService?.Dispose();

        if (!string.IsNullOrEmpty(IconsFolderPath) && Directory.Exists(IconsFolderPath))
        {
            try
            {
                _fsWatcherService =
                    new FileSystemWatcherService(IconsFolderPath, OnIconsDirectoryChanged, OnIconsDirectoryRenamed);
                _fsWatcherService.StartWatching();
                _logger.Information("File system watcher started for: {IconsFolderPath}", IconsFolderPath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error setting up file system watcher for: {IconsFolderPath}", IconsFolderPath);
            }
        }
        else
        {
            _fsWatcherService = null;
            _logger.Warning("File system watcher not set up - invalid path: {IconsFolderPath}",
                IconsFolderPath ?? "null");
        }
    }

    private void OnIconsDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        _logger.Information("Icons directory changed event fired for: {ChangeType} - {FullPath}", e.ChangeType,
            e.FullPath);
        PopulateIconsList(IconsFolderPath);
    }

    private void OnIconsDirectoryRenamed(object sender, RenamedEventArgs e)
    {
        _logger.Information("Icons directory renamed event fired from {OldFullPath} to {FullPath}", e.OldFullPath,
            e.FullPath);
        PopulateIconsList(IconsFolderPath);
    }

    private void LoadPreviousIcons()
    {
        _logger.Information("Loading previous icons from saved location");

        if (string.IsNullOrEmpty(IconsFolderPath))
        {
            _logger.Warning("IconsFolderPath is null or empty, cannot load previous icons");
            return;
        }

        if (!Directory.Exists(IconsFolderPath))
        {
            _logger.Warning("IconsFolderPath does not exist: {IconsFolderPath}", IconsFolderPath);
            return;
        }

        try
        {
            PopulateIconsList(IconsFolderPath);
            ValidateAndSetupFileSystemWatcher();
            _logger.Information("Successfully loaded previous icons");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading previous icons from: {IconsFolderPath}", IconsFolderPath);
        }
    }

    private void FilterIcons()
    {
        try
        {
            var previousCount = FilteredIcons?.Count ?? 0;
            FilteredIcons = _iconManagementService.FilterIcons(Icons, FilterString);
            _logger.Information("Filtered icons: {FilteredCount} of {TotalCount} icons with filter: {FilterString}",
                FilteredIcons.Count, Icons.Count, FilterString ?? "null");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error filtering icons with filter string: {FilterString}", FilterString ?? "null");
        }
    }

    public void RefreshGui()
    {
        _logger.Information("Refreshing GUI, clearing icons and repopulating");

        try
        {
            Icons.Clear();
            PopulateIconsList(IconsFolderPath);
            _logger.Information("GUI refresh completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error refreshing GUI");
        }
    }

    private void UpdateConvertButtonEnabledState()
    {
        var previousState = CanConvertImages;
        CanConvertImages = Icons.Count > 0;

        if (previousState != CanConvertImages)
        {
            _logger.Information("Convert button enabled state changed to: {CanConvertImages} (Icons count: {Count})",
                CanConvertImages, Icons.Count);
        }
    }
}