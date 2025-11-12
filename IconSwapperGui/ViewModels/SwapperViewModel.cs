using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.Swapper;
using IconSwapperGui.Commands.Swapper.ContextMenu;
using IconSwapperGui.Models;
using IconSwapperGui.Services;
using IconSwapperGui.Services.Interfaces;
using IconSwapperGui.ViewModels.Interfaces;
using Serilog;

namespace IconSwapperGui.ViewModels;

public partial class SwapperViewModel : ObservableObject, IIconViewModel
{
    private readonly IApplicationService _applicationService;
    private readonly IIconManagementService _iconManagementService;
    private readonly ILogger _logger = Log.ForContext<SwapperViewModel>();
    public readonly IDialogService DialogService;
    public readonly IElevationService ElevationService;

    [ObservableProperty] private ObservableCollection<Application> _applications;

    private IFileSystemWatcherService? _applicationsDirectoryWatcherService;

    [ObservableProperty] private string? _applicationsFolderPath;

    [ObservableProperty] private bool _canSwapIcons;

    [ObservableProperty] private ObservableCollection<Icon> _filteredIcons;

    [ObservableProperty] private string? _filterString;

    [ObservableProperty] private ObservableCollection<Icon> _icons;

    private FileSystemWatcherService? _iconsDirectoryWatcherService;

    [ObservableProperty] private string? _iconsFolderPath;

    [ObservableProperty] private bool _isTickVisible;

    [ObservableProperty] private Application? _selectedApplication;

    [ObservableProperty] private Icon? _selectedIcon;

    public SwapperViewModel(IApplicationService applicationService, IIconManagementService iconManagementService,
        ISettingsService settingsService, IDialogService dialogService, IElevationService elevationService)
    {
        _logger.Information("SwapperViewModel initializing");

        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _iconManagementService =
            iconManagementService ?? throw new ArgumentNullException(nameof(iconManagementService));
        SettingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        ElevationService = elevationService ?? throw new ArgumentNullException(nameof(elevationService));

        Applications = new ObservableCollection<Application>();
        Icons = new ObservableCollection<Icon>();
        FilteredIcons = new ObservableCollection<Icon>();

        ChooseApplicationShortcutFolderCommand = new ChooseApplicationShortcutFolderCommand(this, null!, _ => true);
        ChooseIconFolderCommand = new ChooseIconFolderCommand<SwapperViewModel>(this, null!, _ => true);
        SwapCommand = new SwapCommand(this, null!, _ => true);
        CopyPathContextCommand = new CopyPathContextCommand(this);
        DeleteIconContextCommand = new DeleteIconContextCommand(this);
        DuplicateIconContextCommand = new DuplicateIconContextCommand(this);
        OpenExplorerContextCommand = new OpenExplorerContextCommand(this);
        ResetIconContextCommand = new ResetIconContextCommand(this);

        LoadPreviousApplications();
        LoadPreviousIcons();
        UpdateSwapButtonEnabledState();

        _logger.Information("SwapperViewModel initialized successfully");
    }

    public RelayCommand ChooseApplicationShortcutFolderCommand { get; }
    public RelayCommand ChooseIconFolderCommand { get; }
    public RelayCommand SwapCommand { get; }
    public RelayCommand CopyPathContextCommand { get; }
    public RelayCommand DeleteIconContextCommand { get; }
    public RelayCommand DuplicateIconContextCommand { get; }
    public RelayCommand OpenExplorerContextCommand { get; }
    public RelayCommand ResetIconContextCommand { get; }

    public ISettingsService SettingsService { get; set; }

    public void PopulateIconsList(string? folderPath)
    {
        _logger.Information("PopulateIconsList called with folderPath: {FolderPath}", folderPath ?? "null");
        var supportedExtensions = new List<string> { ".ico" };

        try
        {
            Icons.Clear();
            var icons = _iconManagementService.GetIcons(folderPath, supportedExtensions);

            var addedCount = 0;
            foreach (var icon in icons)
            {
                if (Icons.Any(x => x.Path == icon.Path))
                {
                    continue;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() => { Icons.Add(icon); });
                addedCount++;
            }

            _logger.Information("Populated icons list with {AddedCount} new icons (total: {TotalCount})", addedCount,
                Icons.Count);

            FilterIcons();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating icons list from folder: {FolderPath}", folderPath);
        }
    }

    partial void OnIconsChanged(ObservableCollection<Icon> value)
    {
        _logger.Information("Icons collection changed, new count: {Count}", value?.Count ?? 0);
        FilterIcons();
    }

    partial void OnIconsFolderPathChanged(string? value)
    {
        _logger.Information("IconsFolderPath changed to: {IconsFolderPath}", value ?? "null");
        SetupIconsDirectoryWatcher();
    }

    partial void OnFilterStringChanged(string? value)
    {
        _logger.Information("FilterString changed to: {FilterString}", value ?? "null");
        FilterIcons();
    }

    partial void OnApplicationsFolderPathChanged(string? value)
    {
        _logger.Information("ApplicationsFolderPath changed to: {ApplicationsFolderPath}", value ?? "null");
        SetupApplicationsDirectoryWatcher();
    }

    partial void OnSelectedApplicationChanged(Application? value)
    {
        _logger.Information("SelectedApplication changed to: {ApplicationName}", value?.Name ?? "null");
        UpdateSwapButtonEnabledState();
    }

    partial void OnSelectedIconChanged(Icon? value)
    {
        _logger.Information("SelectedIcon changed to: {IconName}", value?.Name ?? "null");
        UpdateSwapButtonEnabledState();
    }

    public async Task ShowSuccessTick()
    {
        _logger.Information("Showing success tick");
        IsTickVisible = true;
        await Task.Delay(750);
        IsTickVisible = false;
        _logger.Information("Success tick hidden");
    }

    private void SetupIconsDirectoryWatcher()
    {
        _logger.Information("Setting up icons directory watcher for: {IconsFolderPath}", IconsFolderPath ?? "null");

        try
        {
            _iconsDirectoryWatcherService?.Dispose();

            _iconsDirectoryWatcherService = new FileSystemWatcherService(IconsFolderPath,
                OnIconsDirectoryChanged, OnIconsDirectoryRenamed);
            _iconsDirectoryWatcherService.StartWatching();

            _logger.Information("Icons directory watcher started successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error setting up icons directory watcher for: {IconsFolderPath}", IconsFolderPath);
        }
    }

    private void SetupApplicationsDirectoryWatcher()
    {
        _logger.Information("Setting up applications directory watcher for: {ApplicationsFolderPath}",
            ApplicationsFolderPath ?? "null");

        try
        {
            _applicationsDirectoryWatcherService?.Dispose();

            _applicationsDirectoryWatcherService = new FileSystemWatcherService(ApplicationsFolderPath,
                OnApplicationsDirectoryChanged, OnApplicationsDirectoryRenamed);
            _applicationsDirectoryWatcherService.StartWatching();

            _logger.Information("Applications directory watcher started successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error setting up applications directory watcher for: {ApplicationsFolderPath}",
                ApplicationsFolderPath);
        }
    }

    private void OnIconsDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        _logger.Information("Icons directory changed event fired for: {ChangeType} - {FullPath}", e.ChangeType,
            e.FullPath);
        System.Windows.Application.Current.Dispatcher.Invoke(() => PopulateIconsList(IconsFolderPath));
    }

    private void OnIconsDirectoryRenamed(object sender, RenamedEventArgs e)
    {
        _logger.Information("Icons directory renamed event fired from {OldFullPath} to {FullPath}", e.OldFullPath,
            e.FullPath);
        PopulateIconsList(IconsFolderPath);
    }

    private void OnApplicationsDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        _logger.Information("Applications directory changed event fired for: {ChangeType} - {FullPath}", e.ChangeType,
            e.FullPath);
        PopulateApplicationsList(ApplicationsFolderPath);
    }

    private void OnApplicationsDirectoryRenamed(object sender, RenamedEventArgs e)
    {
        _logger.Information("Applications directory renamed event fired from {OldFullPath} to {FullPath}",
            e.OldFullPath, e.FullPath);
        PopulateApplicationsList(ApplicationsFolderPath);
    }

    private void LoadPreviousApplications()
    {
        _logger.Information("Loading previous applications from saved location");

        ApplicationsFolderPath = SettingsService.GetApplicationsLocation();

        if (string.IsNullOrEmpty(ApplicationsFolderPath))
        {
            _logger.Warning("ApplicationsFolderPath is null or empty, cannot load previous applications");
            return;
        }

        try
        {
            PopulateApplicationsList(ApplicationsFolderPath);
            SetupApplicationsDirectoryWatcher();
            _logger.Information("Successfully loaded previous applications");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading previous applications from: {ApplicationsFolderPath}",
                ApplicationsFolderPath);
        }
    }

    private void LoadPreviousIcons()
    {
        _logger.Information("Loading previous icons from saved location");

        IconsFolderPath = SettingsService.GetIconsLocation();

        if (string.IsNullOrEmpty(IconsFolderPath))
        {
            _logger.Warning("IconsFolderPath is null or empty, cannot load previous icons");
            return;
        }

        try
        {
            PopulateIconsList(IconsFolderPath);
            SetupIconsDirectoryWatcher();
            _logger.Information("Successfully loaded previous icons");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading previous icons from: {IconsFolderPath}", IconsFolderPath);
        }
    }

    public void PopulateApplicationsList(string? folderPath)
    {
        _logger.Information("PopulateApplicationsList called with folderPath: {FolderPath}", folderPath ?? "null");

        try
        {
            Applications.Clear();

            var applications = _applicationService.GetApplications(folderPath);

            var addedCount = 0;
            foreach (var application in applications)
            {
                if (Applications.Any(x => x.Path == application.Path))
                {
                    continue;
                }

                Applications.Add(application);
                addedCount++;
            }

            _logger.Information("Populated applications list with {AddedCount} new applications (total: {TotalCount})",
                addedCount, Applications.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating applications list from folder: {FolderPath}", folderPath);
        }
    }

    public void ResetGui()
    {
        _logger.Information("Resetting GUI");

        try
        {
            var tempSelectedApplicationPath = SelectedApplication?.Path;
            _logger.Information("Preserving selected application: {ApplicationPath}",
                tempSelectedApplicationPath ?? "null");

            Applications.Clear();

            PopulateApplicationsList(ApplicationsFolderPath);

            if (tempSelectedApplicationPath != null)
            {
                SelectedApplication = Applications.FirstOrDefault(app => app.Path == tempSelectedApplicationPath);
                _logger.Information("Restored selected application: {ApplicationName}",
                    SelectedApplication?.Name ?? "null");
            }

            _logger.Information("GUI reset completed successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error resetting GUI");
        }
    }

    public void FilterIcons()
    {
        try
        {
            if (string.IsNullOrEmpty(FilterString))
            {
                FilteredIcons = new ObservableCollection<Icon>(Icons);
                _logger.Information("No filter applied, showing all {Count} icons", Icons.Count);
            }
            else
            {
                var filtered = Icons
                    .Where(icon => icon.Name.Contains(FilterString, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                FilteredIcons = new ObservableCollection<Icon>(filtered);
                _logger.Information(
                    "Filtered icons: {FilteredCount} of {TotalCount} icons match filter '{FilterString}'",
                    FilteredIcons.Count, Icons.Count, FilterString);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error filtering icons with filter string: {FilterString}", FilterString ?? "null");
        }
    }

    private void UpdateSwapButtonEnabledState()
    {
        var previousState = CanSwapIcons;
        CanSwapIcons = SelectedApplication != null && SelectedIcon != null;

        if (previousState != CanSwapIcons)
        {
            _logger.Information(
                "Swap button enabled state changed to: {CanSwapIcons} (Application: {HasApplication}, Icon: {HasIcon})",
                CanSwapIcons, SelectedApplication != null, SelectedIcon != null);
        }
    }
}