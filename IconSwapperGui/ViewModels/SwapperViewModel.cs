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

namespace IconSwapperGui.ViewModels;

public partial class SwapperViewModel : ObservableObject, IIconViewModel
{
    private readonly IApplicationService _applicationService;
    private readonly IIconManagementService _iconManagementService;
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
        var supportedExtensions = new List<string> { ".ico" };

        Icons.Clear();
        var icons = _iconManagementService.GetIcons(folderPath, supportedExtensions);

        foreach (var icon in icons)
        {
            if (Icons.Any(x => x.Path == icon.Path)) continue;

            System.Windows.Application.Current.Dispatcher.Invoke(() => { Icons.Add(icon); });
        }

        FilterIcons();
    }

    partial void OnIconsChanged(ObservableCollection<Icon> value)
    {
        FilterIcons();
    }

    partial void OnIconsFolderPathChanged(string? value)
    {
        SetupIconsDirectoryWatcher();
    }

    partial void OnFilterStringChanged(string? value)
    {
        FilterIcons();
    }

    partial void OnApplicationsFolderPathChanged(string? value)
    {
        SetupApplicationsDirectoryWatcher();
    }

    partial void OnSelectedApplicationChanged(Application? value)
    {
        UpdateSwapButtonEnabledState();
    }

    partial void OnSelectedIconChanged(Icon? value)
    {
        UpdateSwapButtonEnabledState();
    }

    public async Task ShowSuccessTick()
    {
        IsTickVisible = true;
        await Task.Delay(750);
        IsTickVisible = false;
    }

    private void SetupIconsDirectoryWatcher()
    {
        _iconsDirectoryWatcherService?.Dispose();

        _iconsDirectoryWatcherService = new FileSystemWatcherService(IconsFolderPath,
            OnIconsDirectoryChanged, OnIconsDirectoryRenamed);
        _iconsDirectoryWatcherService.StartWatching();
    }

    private void SetupApplicationsDirectoryWatcher()
    {
        _applicationsDirectoryWatcherService?.Dispose();

        _applicationsDirectoryWatcherService = new FileSystemWatcherService(ApplicationsFolderPath,
            OnApplicationsDirectoryChanged, OnApplicationsDirectoryRenamed);
        _applicationsDirectoryWatcherService.StartWatching();
    }

    private void OnIconsDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => PopulateIconsList(IconsFolderPath));
    }

    private void OnIconsDirectoryRenamed(object sender, RenamedEventArgs e)
    {
        PopulateIconsList(IconsFolderPath);
    }

    private void OnApplicationsDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        PopulateApplicationsList(ApplicationsFolderPath);
    }

    private void OnApplicationsDirectoryRenamed(object sender, RenamedEventArgs e)
    {
        PopulateApplicationsList(ApplicationsFolderPath);
    }

    private void LoadPreviousApplications()
    {
        ApplicationsFolderPath = SettingsService.GetApplicationsLocation();

        if (string.IsNullOrEmpty(ApplicationsFolderPath)) return;

        PopulateApplicationsList(ApplicationsFolderPath);

        SetupApplicationsDirectoryWatcher();
    }

    private void LoadPreviousIcons()
    {
        IconsFolderPath = SettingsService.GetIconsLocation();

        if (string.IsNullOrEmpty(IconsFolderPath)) return;

        PopulateIconsList(IconsFolderPath);

        SetupIconsDirectoryWatcher();
    }

    public void PopulateApplicationsList(string? folderPath)
    {
        Applications.Clear();

        var applications = _applicationService.GetApplications(folderPath);

        foreach (var application in applications)
        {
            if (Applications.Any(x => x.Path == application.Path)) continue;
            Applications.Add(application);
        }
    }

    public void ResetGui()
    {
        var tempSelectedApplicationPath = SelectedApplication?.Path;

        Applications.Clear();

        PopulateApplicationsList(ApplicationsFolderPath);

        if (tempSelectedApplicationPath != null)
            SelectedApplication = Applications.FirstOrDefault(app => app.Path == tempSelectedApplicationPath);
    }

    public void FilterIcons()
    {
        if (string.IsNullOrEmpty(FilterString))
        {
            FilteredIcons = new ObservableCollection<Icon>(Icons);
        }
        else
        {
            var filtered = Icons
                .Where(icon => icon.Name.Contains(FilterString, StringComparison.OrdinalIgnoreCase))
                .ToList();
            FilteredIcons = new ObservableCollection<Icon>(filtered);
        }
    }

    private void UpdateSwapButtonEnabledState()
    {
        CanSwapIcons = SelectedApplication != null && SelectedIcon != null;
    }
}