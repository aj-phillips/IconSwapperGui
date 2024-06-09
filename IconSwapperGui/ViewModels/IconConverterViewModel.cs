using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.IconConverter;
using IconSwapperGui.Commands.IconSwapper;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using IconSwapperGui.Services;
using Microsoft.WindowsAPICodePack.Shell.Interop;

namespace IconSwapperGui.ViewModels;

public class IconConverterViewModel : ViewModel, IIconViewModel
{
    public ISettingsService SettingsService { get; set; }
    private readonly IIconService _iconService;
    public IDialogService DialogService { get; set; }

    private ObservableCollection<Icon> _icons;
    private ObservableCollection<Icon> _filteredIcons;
    private string _filterString;
    
    public bool CanDeletePngImages { get; set; }
    public bool _canConvertImages;

    public string IconsFolderPath { get; set; }

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
    
    public bool CanConvertImages
    {
        get => _canConvertImages;
        set
        {
            if (_canConvertImages != value)
            {
                _canConvertImages = value;
                OnPropertyChanged(nameof(CanConvertImages));
            }
        }
    }

    public RelayCommand ConvertIconCommand { get; }
    public RelayCommand ChooseIconFolderCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public IconConverterViewModel(IIconService iconService, ISettingsService settingsService, IDialogService dialogService)
    {
        Icons = new ObservableCollection<Icon>();
        FilteredIcons = new ObservableCollection<Icon>();

        _iconService = iconService;
        SettingsService = settingsService;
        DialogService = dialogService;

        ConvertIconCommand = new ConvertIconCommand(this, null!, x => true); 
        ChooseIconFolderCommand = new ChooseIconFolderCommand<IconConverterViewModel>(this, null!, x => true);
        RefreshCommand = new RefreshIconsCommand(this, null!, x => true);
        
        LoadPreviousIcons();
    }

    private void LoadPreviousIcons()
    {
        IconsFolderPath = SettingsService.GetConverterIconsLocation();

        if (!string.IsNullOrEmpty(IconsFolderPath))
        {
            PopulateIconsList(IconsFolderPath);
        }
    }

    public void PopulateIconsList(string folderPath)
    {
        var icons = _iconService.GetPngIcons(folderPath);

        foreach (var icon in icons)
        {
            if (Icons.Any(x => x.Path == icon.Path)) continue;

            Icons.Add(icon);
        }

        FilterIcons();
        UpdateConvertButtonEnabledState();
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

    public void RefreshGui()
    {
        _icons.Clear();
        PopulateIconsList(IconsFolderPath);
    }

    public void UpdateConvertButtonEnabledState()
    {
        if (_icons.Count == 0)
        {
            CanConvertImages = false;
        }
        else
        {
            CanConvertImages = true;
        }
    }
}