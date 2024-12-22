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

namespace IconSwapperGui.ViewModels;

public partial class ConverterViewModel : ObservableObject, IIconViewModel
{
    private readonly IIconManagementService _iconManagementService;

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
        _iconManagementService = iconService ?? throw new ArgumentNullException(nameof(iconService));
        SettingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        DialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Icons = [];
        FilteredIcons = [];

        ConvertIconCommand = new ConvertIconCommand(this, null!, _ => true);
        ChooseIconFolderCommand = new ChooseIconFolderCommand<ConverterViewModel>(this, null!, _ => true);

        IconsFolderPath = SettingsService.GetConverterIconsLocation();
        ApplicationsLocationPath = SettingsService.GetApplicationsLocation();

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
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;

        Icons = _iconManagementService.GetIcons(folderPath);

        FilterIcons();

        UpdateConvertButtonEnabledState();
    }

    partial void OnIconsFolderPathChanged(string? value)
    {
        ValidateAndSetupFileSystemWatcher();
    }

    partial void OnIconsChanged(ObservableCollection<Icon> value)
    {
        FilterIcons();
    }

    private void ValidateAndSetupFileSystemWatcher()
    {
        _fsWatcherService?.Dispose();

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
        if (string.IsNullOrEmpty(IconsFolderPath) || !Directory.Exists(IconsFolderPath)) return;

        PopulateIconsList(IconsFolderPath);

        ValidateAndSetupFileSystemWatcher();
    }

    private void FilterIcons()
    {
        FilteredIcons = _iconManagementService.FilterIcons(Icons, FilterString);
    }

    public void RefreshGui()
    {
        Icons.Clear();
        PopulateIconsList(IconsFolderPath);
    }

    private void UpdateConvertButtonEnabledState()
    {
        CanConvertImages = Icons.Count > 0;
    }
}