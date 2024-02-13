using IconSwapperGui.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using IconSwapperGui.Models;
using IconSwapperGui.Utilities;
using Microsoft.Win32;

namespace IconSwapperGui.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IApplicationService _applicationService;
    private readonly IIconService _iconService;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(IApplicationService applicationService, IIconService iconService)
    {
        Icons = new ObservableCollection<Icon>();
        FilteredIcons = new ObservableCollection<Icon>();

        _applicationService = applicationService;
        _iconService = iconService;

        ChooseApplicationShortcutFolderCommand = new RelayCommand(_ => ChooseApplicationShortcutFolder());
        ChooseIconFolderCommand = new RelayCommand(_ => ChooseIconFolder());
        SwapCommand = new RelayCommand(_ => SwapIcons());
    }

    private void ChooseApplicationShortcutFolder()
    {
        OpenFolderDialog openFolderDialog = new OpenFolderDialog();

        if (openFolderDialog.ShowDialog() == true)
        {
            string folderPath = openFolderDialog.FolderName;
            var applications = _applicationService.GetApplications(folderPath);

            foreach (var application in applications)
            {
                if (Applications.Any(x => x.Path == application.Path)) continue;

                Applications.Add(application);
            }
        }
    }

    private void ChooseIconFolder()
    {
        OpenFolderDialog openFolderDialog = new OpenFolderDialog();

        if (openFolderDialog.ShowDialog() == true)
        {
            string folderPath = openFolderDialog.FolderName;
            var icons = _iconService.GetIcons(folderPath);

            foreach (var icon in icons)
            {
                if (Icons.Any(x => x.Path == icon.Path)) continue;

                Icons.Add(icon);
            }

            FilterIcons();
        }
    }

    private void SwapIcons()
    {
        // Implement the logic to swap icons for the selected application
    }

    public void FilterIcons()
    {
        if (string.IsNullOrEmpty(_filterString))
        {
            FilteredIcons = new ObservableCollection<Icon>(Icons);
        }
        else
        {
            var filtered = Icons.Where(icon => icon.Name.Contains(_filterString, StringComparison.OrdinalIgnoreCase)).ToList();
            FilteredIcons = new ObservableCollection<Icon>(filtered);
        }
    }

    public string FilterString
    {
        get => _filterString;
        set
        {
            _filterString = value;
            OnPropertyChanged(nameof(FilterString));
            FilterIcons();
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}