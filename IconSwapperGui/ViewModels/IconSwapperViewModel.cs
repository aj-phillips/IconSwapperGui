using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.IconSwapper;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using Application = IconSwapperGui.Models.Application;

namespace IconSwapperGui.ViewModels;

public class IconSwapperViewModel : ViewModel, IIconViewModel
{
    private readonly IApplicationService _applicationService;
    private readonly IIconService _iconService;
    public ISettingsService SettingsService { get; set; }
    public readonly IDialogService DialogService;
    public readonly IElevationService ElevationService;

    private ObservableCollection<Application> _applications;
    private ObservableCollection<Icon> _icons;
    private ObservableCollection<Icon> _filteredIcons;
    private string _filterString;

    public ObservableCollection<Application> Applications
    {
        get => _applications;
        set
        {
            _applications = value;
            OnPropertyChanged(nameof(Applications));
        }
    }

    public ObservableCollection<Icon> Icons
    {
        get => _icons;
        set
        {
            _icons = value;
            OnPropertyChanged(nameof(Icons));
            FilterIcons();
        }
    }

    public ObservableCollection<Icon> FilteredIcons
    {
        get => _filteredIcons;
        private set
        {
            _filteredIcons = value;
            OnPropertyChanged(nameof(FilteredIcons));
        }
    }

    private Application? _selectedApplication;

    public Application? SelectedApplication
    {
        get => _selectedApplication;
        set => SetField(ref _selectedApplication, value);
    }

    private Icon? _selectedIcon;

    public Icon? SelectedIcon
    {
        get => _selectedIcon;
        set => SetField(ref _selectedIcon, value);
    }

    public RelayCommand ChooseApplicationShortcutFolderCommand { get; }
    public RelayCommand ChooseIconFolderCommand { get; }
    public RelayCommand SwapCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public string IconsFolderPath { get; set; }
    public string ApplicationsFolderPath { get; set; }

    public IconSwapperViewModel(IApplicationService applicationService, IIconService iconService,
        ISettingsService settingsService, IDialogService dialogService, IElevationService elevationService)
    {
        Applications = new ObservableCollection<Application>();
        Icons = new ObservableCollection<Icon>();
        FilteredIcons = new ObservableCollection<Icon>();

        _applicationService = applicationService;
        _iconService = iconService;
        SettingsService = settingsService;
        DialogService = dialogService;
        ElevationService = elevationService;

        ChooseApplicationShortcutFolderCommand = new ChooseApplicationShortcutFolderCommand(this, null!, x => true);
        ChooseIconFolderCommand = new ChooseIconFolderCommand<IconSwapperViewModel>(this, null!, x => true);
        SwapCommand = new SwapCommand(this, null!, x => true);
        RefreshCommand = new RefreshCommand(this, null!, x => true);

        LoadPreviousApplications();
        LoadPreviousIcons();
    }

    private void LoadPreviousApplications()
    {
        ApplicationsFolderPath = SettingsService.GetApplicationsLocation();
        
        if (!string.IsNullOrEmpty(ApplicationsFolderPath)) 
        {
            PopulateApplicationsList(ApplicationsFolderPath);
        }
    }

    private void LoadPreviousIcons()
    {
        IconsFolderPath = SettingsService.GetIconsLocation();
        
        if (!string.IsNullOrEmpty(IconsFolderPath))
        {
            PopulateIconsList(IconsFolderPath);
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

    public void PopulateIconsList(string folderPath)
    {
        var icons = _iconService.GetIcons(folderPath);

        foreach (var icon in icons)
        {
            if (Icons.Any(x => x.Path == icon.Path)) continue;

            Icons.Add(icon);
        }

        FilterIcons();
    }

    public void ResetGui()
    {
        SelectedApplication = null;
        SelectedIcon = null;
        FilterIcons();
        Applications.Clear();
        PopulateApplicationsList(ApplicationsFolderPath);
    }

    public void FilterIcons()
    {
        if (string.IsNullOrEmpty(_filterString))
        {
            FilteredIcons = new ObservableCollection<Icon>(Icons);
        }
        else
        {
            var filtered = Icons.Where(icon => icon.Name.Contains(_filterString, StringComparison.OrdinalIgnoreCase))
                .ToList();

            FilteredIcons = new ObservableCollection<Icon>(filtered);
        }
    }

    public string FilterString
    {
        get => _filterString;
        set
        {
            if (_filterString == value) return; 
            _filterString = value;
            OnPropertyChanged(nameof(FilterString));
            FilterIcons();
        }
    }
}