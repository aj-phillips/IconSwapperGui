using IconSwapperGui.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Principal;
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
    private readonly ISettingsService _settingsService;

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

    public event PropertyChangedEventHandler? PropertyChanged;

    public string IconsFolderPath { get; set; }
    public string ApplicationsFolderPath { get; set; }

    public MainViewModel(IApplicationService applicationService, IIconService iconService,
        ISettingsService settingsService)
    {
        Applications = new ObservableCollection<Application>();
        Icons = new ObservableCollection<Icon>();
        FilteredIcons = new ObservableCollection<Icon>();

        _applicationService = applicationService;
        _iconService = iconService;
        _settingsService = settingsService;

        ChooseApplicationShortcutFolderCommand = new RelayCommand(_ => ChooseApplicationShortcutFolder());
        ChooseIconFolderCommand = new RelayCommand(_ => ChooseIconFolder());
        SwapCommand = new RelayCommand(_ => SwapIcons());
        RefreshCommand = new RelayCommand(_ => RefreshAll());

        LoadPreviousApplications();
        LoadPreviousIcons();
    }

    private void LoadPreviousApplications()
    {
        ApplicationsFolderPath = _settingsService.GetApplicationsLocation();

        if (ApplicationsFolderPath != null || ApplicationsFolderPath != "")
        {
            PopulateApplicationsList(ApplicationsFolderPath);
        }
    }

    private void LoadPreviousIcons()
    {
        IconsFolderPath = _settingsService.GetIconsLocation();

        if (IconsFolderPath != null || IconsFolderPath != "")
        {
            PopulateIconsList(IconsFolderPath);
        }
    }

    private void ChooseApplicationShortcutFolder()
    {
        OpenFolderDialog openFolderDialog = new OpenFolderDialog();

        if (openFolderDialog.ShowDialog() == true)
        {
            string folderPath = openFolderDialog.FolderName;

            ApplicationsFolderPath = folderPath;

            PopulateApplicationsList(folderPath);
            _settingsService.SaveApplicationsLocation(ApplicationsFolderPath);
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

            IconsFolderPath = folderPath;

            PopulateIconsList(folderPath);
            _settingsService.SaveIconsLocation(IconsFolderPath);
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
            ShowWarning("Please select an application and an icon to swap.", "No Application or Icon Selected");
            return;
        }

        try
        {
            string extension = Path.GetExtension(SelectedApplication.Path).ToLower();

            switch (extension)
            {
                case ".lnk":
                    SwapLinkFileIcon();
                    break;
                case ".url":
                    SwapUrlFileIcon();
                    break;
            }

            ShowInformation($"The icon for {SelectedApplication.Name} has been successfully swapped.", "Icon Swapped");
            ResetGui();
        }
        catch (Exception ex)
        {
            ShowError($"An error occurred while swapping the icon for {SelectedApplication.Name}: {ex.Message}",
                "Error Swapping Icon");
        }
    }

    private void SwapLinkFileIcon()
    {
        string publicDesktopPath = "C:\\Users\\Public\\Desktop";

        if (Path.GetDirectoryName(SelectedApplication.Path).Equals(publicDesktopPath) && !IsRunningAsAdmin())
        {
            ShowInformation(
                $"To change the icon of {SelectedApplication.Name}, the application needs to be restarted as admin.\n\nYou will need to attempt the swap again afterwards",
                "Permissions Required To Swap Icon");

            ElevateApplicationViaUac();
        }

        var wshShell = (IWshRuntimeLibrary.WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));

        IWshRuntimeLibrary.IWshShortcut shortcut =
            (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(SelectedApplication.Path);

        shortcut.IconLocation = $"{SelectedIcon.Path},0";

        shortcut.Save();
    }

    private void SwapUrlFileIcon()
    {
        string[] urlFileContent = File.ReadAllLines(SelectedApplication.Path);

        for (int i = 0; i < urlFileContent.Length; i++)
        {
            if (urlFileContent[i].StartsWith("IconFile", StringComparison.CurrentCultureIgnoreCase))
            {
                urlFileContent[i] = "IconFile=" + SelectedIcon.Path;
            }
        }

        File.WriteAllLines(SelectedApplication.Path, urlFileContent);
    }

    private void ShowWarning(string message, string caption)
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ShowInformation(string message, string caption)
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowError(string message, string caption)
    {
        MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ResetGui()
    {
        SelectedApplication = null;
        SelectedIcon = null;
        FilterIcons();
        Applications.Clear();
        PopulateApplicationsList(ApplicationsFolderPath);
    }

    private void ElevateApplicationViaUac()
    {
        var processInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            WorkingDirectory = Environment.CurrentDirectory,
            FileName = Process.GetCurrentProcess().MainModule.FileName,
            Verb = "runas",
            Arguments = "elevate"
        };

        try
        {
            Process.Start(processInfo);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "This operation requires elevated permissions. Please run the application as an administrator.",
                "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    private bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        
        var principal = new WindowsPrincipal(identity);
        
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public void RefreshAll()
    {
        Applications.Clear();
        Icons.Clear();

        PopulateApplicationsList(ApplicationsFolderPath);
        PopulateIconsList(IconsFolderPath);
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