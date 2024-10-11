using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.Converter;
using IconSwapperGui.Commands.Swapper;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using IconSwapperGui.Services;

namespace IconSwapperGui.ViewModels;

public class ConverterViewModel : ViewModel, IIconViewModel, INotifyPropertyChanged, IDisposable
{
    private readonly IIconManagementService _iconManagementService;
    private string _applicationsLocationPath;
    private bool _canConvertImages;
    private ObservableCollection<Icon> _filteredIcons;
    private IFileSystemWatcherService _fsWatcherService;

    private ObservableCollection<Icon> _icons;
    private string _iconsFolderPath;

    public ConverterViewModel(
        IIconManagementService iconService,
        ISettingsService settingsService,
        IDialogService dialogService,
        Func<string, Action<object, FileSystemEventArgs>, Action<object, RenamedEventArgs>, IFileSystemWatcherService>
            fileSystemWatcherServiceFactory)
    {
        _iconManagementService = iconService ?? throw new ArgumentNullException(nameof(iconService));
        SettingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Icons = new ObservableCollection<Icon>();
        FilteredIcons = new ObservableCollection<Icon>();

        ConvertIconCommand = new ConvertIconCommand(this, null!, x => true);
        ChooseIconFolderCommand = new ChooseIconFolderCommand<ConverterViewModel>(this, null!, x => true);

        IconsFolderPath = SettingsService.GetConverterIconsLocation();
        ApplicationsLocationPath = SettingsService.GetApplicationsLocation();

        LoadPreviousIcons();
    }

    public IDialogService DialogService { get; set; }
    public RelayCommand ConvertIconCommand { get; }
    public RelayCommand ChooseIconFolderCommand { get; }

    public bool CanDeleteImagesAfterConversion { get; set; }

    public string ApplicationsLocationPath
    {
        get => _applicationsLocationPath;
        set
        {
            if (_applicationsLocationPath != value)
            {
                _applicationsLocationPath = value;
                OnPropertyChanged();
            }
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

    public bool CanConvertImages
    {
        get => _canConvertImages;
        set => SetField(ref _canConvertImages, value);
    }

    public string FilterString { get; set; }

    public void Dispose()
    {
        _fsWatcherService?.Dispose();
    }

    public ISettingsService SettingsService { get; set; }

    public string IconsFolderPath
    {
        get => _iconsFolderPath;
        set
        {
            if (_iconsFolderPath != value)
            {
                _iconsFolderPath = value;
                OnPropertyChanged();
                ValidateAndSetupFileSystemWatcher();
            }
        }
    }

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

    public void PopulateIconsList(string folderPath)
    {
        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
        {
            Icons = _iconManagementService.GetIcons(folderPath);
            FilterIcons();
            UpdateConvertButtonEnabledState();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void ValidateAndSetupFileSystemWatcher()
    {
        if (_fsWatcherService != null) _fsWatcherService.Dispose();

        if (!string.IsNullOrEmpty(IconsFolderPath) && Directory.Exists(IconsFolderPath))
        {
            _fsWatcherService =
                new FileSystemWatcherService(IconsFolderPath, OnIconsDirectoryChanged, OnIconsDirectoryRenamed);
            _fsWatcherService.StartWatching();
        }
        else
        {
            _fsWatcherService = null;
        }
    }

    private void OnIconsDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        PopulateIconsList(IconsFolderPath);
    }

    private void OnIconsDirectoryRenamed(object sender, RenamedEventArgs e)
    {
        PopulateIconsList(IconsFolderPath);
    }

    private void LoadPreviousIcons()
    {
        if (!string.IsNullOrEmpty(IconsFolderPath) && Directory.Exists(IconsFolderPath))
        {
            PopulateIconsList(IconsFolderPath);
            ValidateAndSetupFileSystemWatcher();
        }
    }

    public void FilterIcons()
    {
        FilteredIcons = _iconManagementService.FilterIcons(Icons, FilterString);
    }

    public void RefreshGui()
    {
        Icons.Clear();
        PopulateIconsList(IconsFolderPath);
    }

    public void UpdateConvertButtonEnabledState()
    {
        CanConvertImages = Icons.Count > 0;
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
        return true;
    }
}