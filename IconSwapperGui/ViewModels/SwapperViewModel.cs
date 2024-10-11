using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.Swapper;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using IconSwapperGui.Services;
using Application = IconSwapperGui.Models.Application;

namespace IconSwapperGui.ViewModels;

public class SwapperViewModel : ViewModel, IIconViewModel, INotifyPropertyChanged, IDisposable
{
    private ObservableCollection<Application> _applications;
    private string _applicationsFolderPath;
    private bool _isTickVisible;
    private bool _canSwapIcons;
    private ObservableCollection<Icon> _filteredIcons;
    private string _filterString;
    private ObservableCollection<Icon> _icons;

    private IFileSystemWatcherService _iconsDirectoryWatcherService;
    private IFileSystemWatcherService _applicationsDirectoryWatcherService;

    private string _iconsFolderPath;
    private Application? _selectedApplication;
    private Icon? _selectedIcon;

    private readonly IApplicationService _applicationService;
    private readonly IIconManagementService _iconManagementService;
    public readonly IDialogService DialogService;
    public readonly IElevationService ElevationService;

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

        ChooseApplicationShortcutFolderCommand = new ChooseApplicationShortcutFolderCommand(this, null!, x => true);
        ChooseIconFolderCommand = new ChooseIconFolderCommand<SwapperViewModel>(this, null!, x => true);
        SwapCommand = new SwapCommand(this, null!, x => true);

        LoadPreviousApplications();
        LoadPreviousIcons();
        UpdateSwapButtonEnabledState();
    }

    public ObservableCollection<Application> Applications
    {
        get => _applications;
        set
        {
            _applications = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Icon> FilteredIcons
    {
        get => _filteredIcons;
        private set
        {
            _filteredIcons = value;
            OnPropertyChanged();
        }
    }

    public Application? SelectedApplication
    {
        get => _selectedApplication;
        set => SetField(ref _selectedApplication, value);
    }

    public Icon? SelectedIcon
    {
        get => _selectedIcon;
        set => SetField(ref _selectedIcon, value);
    }

    public bool CanSwapIcons
    {
        get => _canSwapIcons;
        set => SetField(ref _canSwapIcons, value);
    }

    public bool IsTickVisible
    {
        get => _isTickVisible;
        set => SetField(ref _isTickVisible, value);
    }

    public async Task ShowSuccessTick()
    {
        IsTickVisible = true;
        await Task.Delay(750);
        IsTickVisible = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    
    public RelayCommand ChooseApplicationShortcutFolderCommand { get; }
    public RelayCommand ChooseIconFolderCommand { get; }
    public RelayCommand SwapCommand { get; }

    public string ApplicationsFolderPath
    {
        get => _applicationsFolderPath;
        set
        {
            if (_applicationsFolderPath != value)
            {
                _applicationsFolderPath = value;
                OnPropertyChanged();
                SetupApplicationsDirectoryWatcher();
            }
        }
    }

    public string FilterString
    {
        get => _filterString;
        set
        {
            if (_filterString == value) return;
            _filterString = value;
            OnPropertyChanged();
            FilterIcons();
        }
    }

    public void Dispose()
    {
        _iconsDirectoryWatcherService?.Dispose();
        _applicationsDirectoryWatcherService?.Dispose();
    }

    public ISettingsService SettingsService { get; set; }

    public ObservableCollection<Icon> Icons
    {
        get => _icons;
        set
        {
            _icons = value;
            OnPropertyChanged();
            FilterIcons();
        }
    }

    public string IconsFolderPath
    {
        get => _iconsFolderPath;
        set
        {
            if (_iconsFolderPath != value)
            {
                _iconsFolderPath = value;
                OnPropertyChanged();
                SetupIconsDirectoryWatcher();
            }
        }
    }

    public void PopulateIconsList(string folderPath)
    {
        var supportedExtensions = new List<string> { ".ico" };

        Icons.Clear();
        var icons = _iconManagementService.GetIcons(folderPath, supportedExtensions);

        foreach (var icon in icons)
        {
            if (Icons.Any(x => x.Path == icon.Path)) continue;
            Icons.Add(icon);
        }

        FilterIcons();
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
        PopulateIconsList(IconsFolderPath);
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

        if (!string.IsNullOrEmpty(ApplicationsFolderPath))
        {
            PopulateApplicationsList(ApplicationsFolderPath);
            SetupApplicationsDirectoryWatcher();
        }
    }

    private void LoadPreviousIcons()
    {
        IconsFolderPath = SettingsService.GetIconsLocation();

        if (!string.IsNullOrEmpty(IconsFolderPath))
        {
            PopulateIconsList(IconsFolderPath);
            SetupIconsDirectoryWatcher();
        }
    }

    public void PopulateApplicationsList(string folderPath)
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
        {
            SelectedApplication = Applications.FirstOrDefault(app => app.Path == tempSelectedApplicationPath);
        }
    }

    public void FilterIcons()
    {
        if (string.IsNullOrEmpty(_filterString))
        {
            FilteredIcons = new ObservableCollection<Icon>(Icons);
        }
        else
        {
            var filtered = Icons
                .Where(icon => icon.Name.Contains(_filterString, StringComparison.OrdinalIgnoreCase))
                .ToList();
            FilteredIcons = new ObservableCollection<Icon>(filtered);
        }
    }

    public void UpdateSwapButtonEnabledState()
    {
        CanSwapIcons = SelectedApplication != null && SelectedIcon != null;
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        if (propertyName == nameof(SelectedApplication) || propertyName == nameof(SelectedIcon))
            UpdateSwapButtonEnabledState();

        return true;
    }
}