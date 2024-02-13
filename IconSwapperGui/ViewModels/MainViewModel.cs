using IconSwapperGui.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using IconSwapperGui.Models;
using IconSwapperGui.Utilities;
using Microsoft.Win32;
using Application = IconSwapperGui.Models.Application;

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

    public string iconsFolderPath { get; set; }
    public string applicationsFolderPath { get; set; }

    public MainViewModel(IApplicationService applicationService, IIconService iconService)
    {
        Applications = new ObservableCollection<Application>();
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

            applicationsFolderPath = folderPath;

            PopulateApplicationsList(folderPath);
        }
    }

    private void PopulateApplicationsList(string folderPath)
    {
        Applications.Clear();

        var applications = _applicationService.GetApplications(folderPath);

        foreach (var application in applications)
        {
            if (Applications.Any(x => x.Path == application.Path)) continue;

            Applications.Add(application);
        }
    }

    private void ChooseIconFolder()
    {
        Icons.Clear();

        OpenFolderDialog openFolderDialog = new OpenFolderDialog();

        if (openFolderDialog.ShowDialog() == true)
        {
            string folderPath = openFolderDialog.FolderName;

            iconsFolderPath = folderPath;

            PopulateIconsList(folderPath);
        }
    }

    private void PopulateIconsList(string folderPath)
    {
        var icons = _iconService.GetIcons(folderPath);

        foreach (var icon in icons)
        {
            if (Icons.Any(x => x.Path == icon.Path)) continue;

            Icons.Add(icon);
        }

        FilterIcons();
    }

    private void SwapIcons()
    {
        if (SelectedApplication == null || SelectedIcon == null)
        {
            MessageBox.Show("Please select an application and an icon to swap.", "No Application or Icon Selected",
                               MessageBoxButton.OK, MessageBoxImage.Warning);

            return;
        }

        try
        {
            var wshShell = (IWshRuntimeLibrary.WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));

            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(SelectedApplication.Path);

            shortcut.IconLocation = $"{SelectedIcon.Path},0";

            shortcut.Save();

            MessageBox.Show($"The icon for {SelectedApplication.Name} has been successfully swapped.", "Icon Swapped",
                                              MessageBoxButton.OK, MessageBoxImage.Information);

            SelectedApplication = null;
            SelectedIcon = null;

            FilterIcons();

            Applications.Clear();
            PopulateApplicationsList(applicationsFolderPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while swapping the icon for {SelectedApplication.Name}: {ex.Message}",
                                              "Error Swapping Icon", MessageBoxButton.OK, MessageBoxImage.Error);
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