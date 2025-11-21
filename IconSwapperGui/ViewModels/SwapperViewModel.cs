using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.Swapper;
using IconSwapperGui.Commands.Swapper.ContextMenu;
using IconSwapperGui.Models;
using IconSwapperGui.Services;
using IconSwapperGui.Services.Interfaces;
using IconSwapperGui.ViewModels.Interfaces;
using IconSwapperGui.Windows;
using Serilog;

namespace IconSwapperGui.ViewModels;

public partial class SwapperViewModel : ObservableObject, IIconViewModel
{
    private readonly IApplicationService _applicationService;
    private readonly IIconManagementService _iconManagementService;
    private readonly IIconHistoryService _iconHistoryService;
    private readonly ILogger _logger = Log.ForContext<SwapperViewModel>();
    public readonly IDialogService DialogService;
    public readonly IElevationService ElevationService;

    public ISettingsService SettingsService { get; set; }

    [ObservableProperty] private ObservableCollection<Application> _applications;
    [ObservableProperty] private ObservableCollection<string> _applicationsFolders;

    private List<IFileSystemWatcherService>? _applicationsDirectoryWatcherServices;

    [ObservableProperty] private string? _applicationsFolderPath;

    [ObservableProperty] private bool _canSwapIcons;

    [ObservableProperty] private ObservableCollection<Icon> _filteredIcons;

    [ObservableProperty] private string? _filterString;
    private CancellationTokenSource? _filterCts;

    [ObservableProperty] private ObservableCollection<Icon> _icons;

    [ObservableProperty] private ObservableCollection<string> _iconsFolders;

    private List<IFileSystemWatcherService>? _iconsDirectoryWatcherServices;

    [ObservableProperty] private string? _iconsFolderPath;

    [ObservableProperty] private ObservableCollection<FolderItem> _folders;
    [ObservableProperty] private ObservableCollection<string> _foldersFolders;
    private List<IFileSystemWatcherService>? _foldersDirectoryWatcherServices;

    [ObservableProperty] private string? _foldersFolderPath;

    [ObservableProperty] private FolderItem? _selectedFolder;

    [ObservableProperty] private bool _isFolderTabSelected;

    [ObservableProperty] private bool _isTickVisible;

    [ObservableProperty] private bool _canSwapFolderIcons;

    [ObservableProperty] private Application? _selectedApplication;

    [ObservableProperty] private Icon? _selectedIcon;

    public SwapperViewModel(IApplicationService applicationService, IIconManagementService iconManagementService,
        ISettingsService settingsService, IDialogService dialogService, IElevationService elevationService, IIconHistoryService iconHistoryService)
    {
        _logger.Information("SwapperViewModel initializing");

        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
        _iconManagementService = iconManagementService ?? throw new ArgumentNullException(nameof(iconManagementService));
        _iconHistoryService = iconHistoryService ?? throw new ArgumentNullException(nameof(iconHistoryService));
        SettingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        ElevationService = elevationService ?? throw new ArgumentNullException(nameof(elevationService));

        Applications = new ObservableCollection<Application>();
        ApplicationsFolders = new ObservableCollection<string>();
        Icons = new ObservableCollection<Icon>();
        IconsFolders = new ObservableCollection<string>();
        Folders = new ObservableCollection<FolderItem>();
        FilteredIcons = new ObservableCollection<Icon>();

        ChooseApplicationShortcutFolderCommand = new ChooseApplicationShortcutFolderCommand(this, null!, _ => true);
        ChooseIconFolderCommand = new ChooseIconFolderCommand<SwapperViewModel>(this, null!, _ => true);
        ChooseFoldersFolderCommand = new ChooseFoldersFolderCommand(this, null!, _ => true);
        SwapFolderIconCommand = new SwapFolderIconCommand(this, new FolderService(), iconHistoryService, null!, _ => true);
        SwapCommand = new SwapCommand(this, null!, _ => true, iconHistoryService);
        DualSwapCommand = new RelayCommand(_ => ExecuteDualSwap(), _ => CanSwap);
        CopyPathContextCommand = new CopyPathContextCommand(this);
        DeleteIconContextCommand = new DeleteIconContextCommand(this);
        DuplicateIconContextCommand = new DuplicateIconContextCommand(this);
        OpenExplorerContextCommand = new OpenExplorerContextCommand(this);
        ManageVersionsContextCommand = new RelayCommand(async _ => await OpenVersionManagerAsync(), _ => SelectedApplication != null);
        ManageDirectoriesCommand = new RelayCommand(_ => OpenManageDirectories(), _ => true);

        SettingsService = settingsService;

        Services.SettingsService.LocationsChanged += () => System.Windows.Application.Current.Dispatcher.Invoke(RefreshGui);

        LoadPreviousApplications();
        LoadPreviousIcons();
        LoadPreviousFolders();
        UpdateSwapButtonEnabledState();

        _logger.Information("SwapperViewModel initialized successfully");
    }

    public RelayCommand ChooseApplicationShortcutFolderCommand { get; }
    public RelayCommand ChooseIconFolderCommand { get; }
    public RelayCommand ChooseFoldersFolderCommand { get; }
    public RelayCommand SwapFolderIconCommand { get; }
    public RelayCommand SwapCommand { get; }
    public RelayCommand DualSwapCommand { get; }
    public RelayCommand CopyPathContextCommand { get; }
    public RelayCommand DeleteIconContextCommand { get; }
    public RelayCommand DuplicateIconContextCommand { get; }
    public RelayCommand OpenExplorerContextCommand { get; }
    public RelayCommand ManageDirectoriesCommand { get; }
    public ICommand ManageVersionsContextCommand { get; }

    partial void OnSelectedApplicationChanged(Application? value)
    {
        UpdateSwapButtonEnabledState();
        CommandManager.InvalidateRequerySuggested();
    }

    partial void OnSelectedIconChanged(Icon? value)
    {
        UpdateSwapButtonEnabledState();
        CommandManager.InvalidateRequerySuggested();
    }

    partial void OnSelectedFolderChanged(FolderItem? value)
    {
        UpdateSwapButtonEnabledState();
        CommandManager.InvalidateRequerySuggested();
    }

    partial void OnFilterStringChanged(string? value)
    {
        try
        {
            _filterCts?.Cancel();
            _filterCts?.Dispose();
            _filterCts = new CancellationTokenSource();
            var token = _filterCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(300, token).ConfigureAwait(false);
                    if (token.IsCancellationRequested) return;
                    System.Windows.Application.Current.Dispatcher.Invoke(() => FilterIcons());
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Error applying debounced filter: {FilterString}", value);
                }
            }, token);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error scheduling debounced filter: {FilterString}", value);
        }
    }

    public void RefreshGui()
    {
        _logger.Information("Refreshing GUI, clearing icons/folders and repopulating");

        try
        {
            var prevSelectedApplicationPath = SelectedApplication?.Path;
            var prevSelectedIconPath = SelectedIcon?.Path;
            var prevSelectedFolderPath = SelectedFolder?.Path;

            Applications.Clear();
            Icons.Clear();
            Folders.Clear();

            var appsList = new List<string>();
            var apps = SettingsService.GetApplicationsLocations();

            if (apps != null && apps.Any()) 
                appsList.AddRange(apps);

            var singleApp = SettingsService.GetApplicationsLocation();

            if (!string.IsNullOrWhiteSpace(singleApp) && !appsList.Contains(singleApp)) 
                appsList.Add(singleApp);

            if (appsList.Any())
            {
                ApplicationsFolders = new ObservableCollection<string>(appsList);
                PopulateApplicationsFromLocations(ApplicationsFolders);
                ApplicationsFolderPath = ApplicationsFolders.FirstOrDefault();
                SetupApplicationsDirectoryWatcher();
            }
            else
            {
                ApplicationsFolderPath = SettingsService.GetApplicationsLocation();

                if (!string.IsNullOrEmpty(ApplicationsFolderPath))
                {
                    PopulateApplicationsList(ApplicationsFolderPath);
                    SetupApplicationsDirectoryWatcher();
                }
            }

            var iconsList = new List<string>();
            var icons = SettingsService.GetIconsLocations();

            if (icons != null && icons.Any()) 
                iconsList.AddRange(icons);

            var singleIcon = SettingsService.GetIconsLocation();

            if (!string.IsNullOrWhiteSpace(singleIcon) && !iconsList.Contains(singleIcon)) 
                iconsList.Add(singleIcon);

            if (iconsList.Any())
            {
                IconsFolders = new ObservableCollection<string>(iconsList);
                PopulateIconsFromLocations(IconsFolders);
                IconsFolderPath = IconsFolders.FirstOrDefault();
                SetupIconsDirectoryWatcher();
            }
            else
            {
                IconsFolderPath = SettingsService.GetIconsLocation();
                if (!string.IsNullOrEmpty(IconsFolderPath))
                {
                    PopulateIconsList(IconsFolderPath);
                    SetupIconsDirectoryWatcher();
                }
            }

            var foldersList = new List<string>();
            var folders = SettingsService.GetFoldersLocations();

            if (folders != null && folders.Any()) 
                foldersList.AddRange(folders);

            var singleFolder = SettingsService.GetFoldersLocation();

            if (!string.IsNullOrWhiteSpace(singleFolder) && !foldersList.Contains(singleFolder)) 
                foldersList.Add(singleFolder);

            if (foldersList.Any())
            {
                FoldersFolders = new ObservableCollection<string>(foldersList);
                PopulateFoldersFromLocations(FoldersFolders);
                FoldersFolderPath = FoldersFolders.FirstOrDefault();
                SetupFoldersDirectoryWatcher();
            }
            else if (!string.IsNullOrEmpty(FoldersFolderPath))
            {
                PopulateFoldersList(FoldersFolderPath);
                SetupFoldersDirectoryWatcher();
            }

            if (prevSelectedApplicationPath != null)
            {
                SelectedApplication = Applications.FirstOrDefault(app => app.Path == prevSelectedApplicationPath);
            }

            if (prevSelectedIconPath != null)
            {
                SelectedIcon = Icons.FirstOrDefault(icon => icon.Path == prevSelectedIconPath);
            }

            if (prevSelectedFolderPath != null)
            {
                SelectedFolder = Folders.FirstOrDefault(f => f.Path == prevSelectedFolderPath);
            }

            UpdateSwapButtonEnabledState();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error refreshing GUI");
        }
    }

    public void PopulateIconsList(string? folderPath)
    {
        _logger.Information("PopulateIconsList called with folderPath: {FolderPath}", folderPath ?? "null");
        var supportedExtensions = new List<string> { ".ico" };

        try
        {
            var icons = _iconManagementService.GetIcons(folderPath, supportedExtensions);
            var addedCount = 0;

            foreach (var icon in icons)
            {
                if (Icons.Any(x => x.Path == icon.Path)) continue;
                Icons.Add(icon);
                addedCount++;
            }

            _logger.Information("Populated icons list with {AddedCount} new icons (total: {TotalCount})", addedCount, Icons.Count);
            FilterIcons();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating icons list from folder: {FolderPath}", folderPath);
        }
    }

    public void PopulateIconsFromLocations(IEnumerable<string>? folderPaths)
    {
        _logger.Information("PopulateIconsFromLocations called");
        var supportedExtensions = new List<string> { ".ico" };

        try
        {
            foreach (var folderPath in folderPaths ?? Enumerable.Empty<string>())
            {
                var icons = _iconManagementService.GetIcons(folderPath, supportedExtensions);

                foreach (var icon in icons)
                {
                    if (Icons.Any(x => x.Path == icon.Path)) continue;
                    Icons.Add(icon);
                }
            }

            FilterIcons();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating icons from locations");
        }
    }

    public void PopulateApplicationsList(string? folderPath)
    {
        _logger.Information("PopulateApplicationsList called with folderPath: {FolderPath}", folderPath ?? "null");

        try
        {
            var applications = _applicationService.GetApplications(folderPath);

            foreach (var application in applications)
            {
                if (Applications.Any(x => x.Path == application.Path)) continue;
                Applications.Add(application);
            }

            _logger.Information("Populated applications list (total: {TotalCount})", Applications.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating applications list from folder: {FolderPath}", folderPath);
        }
    }

    public void PopulateApplicationsFromLocations(IEnumerable<string>? folderPaths)
    {
        _logger.Information("PopulateApplicationsFromLocations called");

        try
        {
            foreach (var folderPath in folderPaths ?? Enumerable.Empty<string>())
            {
                var applications = _applicationService.GetApplications(folderPath);

                foreach (var app in applications)
                {
                    if (Applications.Any(x => x.Path == app.Path)) continue;
                    Applications.Add(app);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating applications from locations");
        }
    }

    public void PopulateFoldersList(string? folderPath)
    {
        _logger.Information("PopulateFoldersList called with folderPath: {FolderPath}", folderPath ?? "null");

        try
        {
            var folderService = new FolderService();
            var folders = folderService.GetFolders(folderPath);

            foreach (var f in folders)
            {
                if (Folders.Any(x => x.Path == f.Path)) continue;
                Folders.Add(f);
            }

            _logger.Information("Populated folders list (total: {Total})", Folders.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating folders list from folder: {FolderPath}", folderPath);
        }
    }

    public void PopulateFoldersFromLocations(IEnumerable<string>? folderPaths)
    {
        _logger.Information("PopulateFoldersFromLocations called");

        try
        {
            var folderService = new FolderService();

            foreach (var folderPath in folderPaths ?? Enumerable.Empty<string>())
            {
                var folders = folderService.GetFolders(folderPath);

                foreach (var f in folders)
                {
                    if (Folders.Any(x => x.Path == f.Path)) continue;
                    Folders.Add(f);
                }
            }

            _logger.Information("Populated folders from locations (total: {Total})", Folders.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error populating folders from locations");
        }
    }

    public void FilterIcons()
    {
        try
        {
            FilteredIcons ??= new ObservableCollection<Icon>();
            FilteredIcons.Clear();

            if (string.IsNullOrWhiteSpace(FilterString))
            {
                foreach (var icon in Icons) FilteredIcons.Add(icon);
            }
            else
            {
                var filter = FilterString.Trim();
                var filtered = Icons.Where(icon => icon.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

                foreach (var icon in filtered) 
                    FilteredIcons.Add(icon);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error filtering icons");
        }
    }

    private void UpdateSwapButtonEnabledState()
    {
        CanSwapIcons = SelectedApplication != null && SelectedIcon != null;
        CanSwapFolderIcons = SelectedFolder != null && SelectedIcon != null;
    }

    public bool CanSwap => (!IsFolderTabSelected && CanSwapIcons) || (IsFolderTabSelected && CanSwapFolderIcons);

    private void ExecuteDualSwap()
    {
        try
        {
            if (IsFolderTabSelected)
            {
                SwapFolderIconCommand?.Execute(null);
            }
            else
            {
                SwapCommand?.Execute(null);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing dual swap");
        }
    }

    public Task<string?> GetCurrentIconPathAsync(string filePath)
    {
        return _iconManagementService.GetCurrentIconPathAsync(filePath);
    }

    public async Task ShowSuccessTick()
    {
        try
        {
            IsTickVisible = true;
            await Task.Delay(800).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error showing success tick");
        }
        finally
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => { IsTickVisible = false; });
        }
    }

    public void ResetGui()
    {
        try
        {
            SelectedIcon = null;
            SelectedApplication = null;
            SelectedFolder = null;
            FilterString = null;
            RefreshGui();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error resetting GUI");
        }
    }

    private Task OpenVersionManagerAsync()
    {
        if (SelectedApplication == null) return Task.CompletedTask;

        try
        {
            var viewModel = new IconVersionManagerViewModel(_iconHistoryService, DialogService, SelectedApplication.Path);
            var window = new IconVersionManagerWindow(viewModel.FilePath) { Owner = System.Windows.Application.Current.MainWindow };
            var result = window.ShowDialog();

            if (result == true) 
                LoadPreviousApplications();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error opening version manager");
            DialogService.ShowError("Error", "Failed to open version manager");
        }

        return Task.CompletedTask;
    }

    private void OpenManageDirectories()
    {
        try
        {
            var vm = new ManageDirectoriesViewModel(SettingsService);

            vm.LocationsChanged = () => System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LoadPreviousIcons();
                LoadPreviousApplications();
                LoadPreviousFolders();
            });

            var window = new ManageDirectoriesWindow(vm) { Owner = System.Windows.Application.Current.MainWindow };

            vm.CloseAction = () => { window.DialogResult = true; window.Close(); };

            var result = window.ShowDialog();

            if (result != true) return;

            LoadPreviousIcons();
            LoadPreviousApplications();
            LoadPreviousFolders();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error opening Manage Directories window");
            DialogService.ShowError("Error", "Failed to open manage directories window");
        }
    }

    private void SetupIconsDirectoryWatcher()
    {
        try
        {
            if (_iconsDirectoryWatcherServices != null)
            {
                foreach (var svc in _iconsDirectoryWatcherServices) svc.Dispose();
                _iconsDirectoryWatcherServices = null;
            }

            var locations = IconsFolders?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();

            if (!string.IsNullOrWhiteSpace(IconsFolderPath) && Directory.Exists(IconsFolderPath) && !locations.Contains(IconsFolderPath))
                locations.Add(IconsFolderPath);

            if (locations.Any())
            {
                _iconsDirectoryWatcherServices = new List<IFileSystemWatcherService>();

                foreach (var loc in locations)
                {
                    if (!Directory.Exists(loc)) continue;

                    var svc = new FileSystemWatcherService(loc, OnIconsDirectoryChanged, OnIconsDirectoryRenamed);

                    svc.StartWatching();

                    _iconsDirectoryWatcherServices.Add(svc);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error setting up icons watcher");
        }
    }

    private void SetupApplicationsDirectoryWatcher()
    {
        try
        {
            if (_applicationsDirectoryWatcherServices != null)
            {
                foreach (var svc in _applicationsDirectoryWatcherServices) svc.Dispose();
                _applicationsDirectoryWatcherServices = null;
            }

            var locations = ApplicationsFolders?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();

            if (!string.IsNullOrWhiteSpace(ApplicationsFolderPath) && Directory.Exists(ApplicationsFolderPath) && !locations.Contains(ApplicationsFolderPath))
                locations.Add(ApplicationsFolderPath);

            if (locations.Any())
            {
                _applicationsDirectoryWatcherServices = new List<IFileSystemWatcherService>();

                foreach (var loc in locations)
                {
                    if (!Directory.Exists(loc)) continue;
                    var svc = new FileSystemWatcherService(loc, OnApplicationsDirectoryChanged, OnApplicationsDirectoryRenamed);
                    svc.StartWatching();
                    _applicationsDirectoryWatcherServices.Add(svc);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error setting up applications watcher");
        }
    }

    private void SetupFoldersDirectoryWatcher()
    {
        try
        {
            if (_foldersDirectoryWatcherServices != null)
            {
                foreach (var svc in _foldersDirectoryWatcherServices) svc.Dispose();
                _foldersDirectoryWatcherServices = null;
            }

            var locations = FoldersFolders?.Where(p => !string.IsNullOrWhiteSpace(p)).ToList() ?? new List<string>();

            if (!string.IsNullOrWhiteSpace(FoldersFolderPath) && Directory.Exists(FoldersFolderPath) && !locations.Contains(FoldersFolderPath))
                locations.Add(FoldersFolderPath);

            if (locations.Any())
            {
                _foldersDirectoryWatcherServices = new List<IFileSystemWatcherService>();

                foreach (var loc in locations)
                {
                    if (!Directory.Exists(loc)) continue;
                    var svc = new FileSystemWatcherService(loc, OnFoldersDirectoryChanged, OnFoldersDirectoryRenamed);
                    svc.StartWatching();
                    _foldersDirectoryWatcherServices.Add(svc);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error setting up folders watcher");
        }
    }

    private void OnIconsDirectoryChanged(object sender, FileSystemEventArgs e) => System.Windows.Application.Current.Dispatcher.Invoke(() => {
        PopulateIconsFromLocations(IconsFolders);

        if (!string.IsNullOrWhiteSpace(IconsFolderPath))
            PopulateIconsList(IconsFolderPath);
    });

    private void OnIconsDirectoryRenamed(object sender, RenamedEventArgs e) => PopulateIconsFromLocations(IconsFolders);

    private void OnApplicationsDirectoryChanged(object sender, FileSystemEventArgs e) => System.Windows.Application.Current.Dispatcher.Invoke(() => {
        PopulateApplicationsFromLocations(ApplicationsFolders);

        if (!string.IsNullOrWhiteSpace(ApplicationsFolderPath)) 
            PopulateApplicationsList(ApplicationsFolderPath);
    });

    private void OnApplicationsDirectoryRenamed(object sender, RenamedEventArgs e) => PopulateApplicationsFromLocations(ApplicationsFolders);

    private void OnFoldersDirectoryChanged(object sender, FileSystemEventArgs e) => System.Windows.Application.Current.Dispatcher.Invoke(() => {
        PopulateFoldersFromLocations(FoldersFolders);

        if (!string.IsNullOrWhiteSpace(FoldersFolderPath)) 
            PopulateFoldersList(FoldersFolderPath);
    });

    private void OnFoldersDirectoryRenamed(object sender, RenamedEventArgs e) => PopulateFoldersFromLocations(FoldersFolders);

    private void LoadPreviousApplications()
    {
        _logger.Information("Loading previous applications from saved location");

        var appsList = new List<string>();
        var savedList = SettingsService.GetApplicationsLocations();

        if (savedList != null && savedList.Any()) 
            appsList.AddRange(savedList);

        var single = SettingsService.GetApplicationsLocation();

        if (!string.IsNullOrWhiteSpace(single) && !appsList.Contains(single))
            appsList.Add(single);

        if (appsList.Any())
        {
            ApplicationsFolders = new ObservableCollection<string>(appsList);
            PopulateApplicationsFromLocations(ApplicationsFolders);
            ApplicationsFolderPath = ApplicationsFolders.FirstOrDefault();
            SetupApplicationsDirectoryWatcher();
            return;
        }
    }

    private void LoadPreviousIcons()
    {
        _logger.Information("Loading previous icons from saved location");

        var iconsList = new List<string>();
        var saved = SettingsService.GetIconsLocations();

        if (saved != null && saved.Any()) 
            iconsList.AddRange(saved);

        var single = SettingsService.GetIconsLocation();

        if (!string.IsNullOrWhiteSpace(single) && !iconsList.Contains(single))
            iconsList.Add(single);

        if (iconsList.Any())
        {
            IconsFolders = new ObservableCollection<string>(iconsList);
            PopulateIconsFromLocations(IconsFolders);
            IconsFolderPath = IconsFolders.FirstOrDefault();
            SetupIconsDirectoryWatcher();
            return;
        }

        IconsFolderPath = SettingsService.GetIconsLocation();

        if (!string.IsNullOrWhiteSpace(IconsFolderPath))
        {
            PopulateIconsList(IconsFolderPath);
            SetupIconsDirectoryWatcher();
        }
    }

    private void LoadPreviousFolders()
    {
        _logger.Information("Loading previous folders from saved location");

        var foldersList = new List<string>();
        var saved = SettingsService.GetFoldersLocations();

        if (saved != null && saved.Any()) 
            foldersList.AddRange(saved);

        var single = SettingsService.GetFoldersLocation();

        if (!string.IsNullOrWhiteSpace(single) && !foldersList.Contains(single))
            foldersList.Add(single);

        if (foldersList.Any())
        {
            FoldersFolders = new ObservableCollection<string>(foldersList);
            PopulateFoldersFromLocations(FoldersFolders);
            FoldersFolderPath = FoldersFolders.FirstOrDefault();
            SetupFoldersDirectoryWatcher();
            return;
        }

        FoldersFolderPath = SettingsService.GetFoldersLocation();

        if (!string.IsNullOrWhiteSpace(FoldersFolderPath))
        {
            PopulateFoldersList(FoldersFolderPath);
            SetupFoldersDirectoryWatcher();
        }
    }
}