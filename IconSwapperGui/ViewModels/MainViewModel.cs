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

    public ObservableCollection<Application> Applications { get; } = new ObservableCollection<Application>();
    public ObservableCollection<Icon> Icons { get; } = new ObservableCollection<Icon>();

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
        _applicationService = applicationService;
        _iconService = iconService;

        ChooseApplicationShortcutFolderCommand = new RelayCommand(_ => ChooseApplicationShortcutFolder());
        ChooseIconFolderCommand = new RelayCommand(_ => ChooseIconFolder());
        SwapCommand = new RelayCommand(_ => SwapIcons());
    }

    private void ChooseApplicationShortcutFolder()
    {
        // Use a dialog to let the user choose a folder
        // Use _applicationService to populate Applications based on the chosen folder
        OpenFolderDialog openFolderDialog = new OpenFolderDialog();

        if (openFolderDialog.ShowDialog() == true)
        {
            string folderPath = openFolderDialog.FolderName;
            var applications = _applicationService.GetApplications(folderPath);

            foreach (var application in applications)
            {
                Applications.Add(application);
            }
        }
    }

    private void ChooseIconFolder()
    {
        // Use a dialog to let the user choose a folder
        // Use _iconService to populate Icons based on the chosen folder
    }

    private void SwapIcons()
    {
        // Implement the logic to swap icons for the selected application
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