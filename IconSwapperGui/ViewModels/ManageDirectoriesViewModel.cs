using System.Collections.ObjectModel;
using IconSwapperGui.Commands;
using IconSwapperGui.Services.Interfaces;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace IconSwapperGui.ViewModels;

public class ManageDirectoriesViewModel
{
    private readonly ISettingsService _settingsService;

    public ManageDirectoriesViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        IconsFolders = new ObservableCollection<string>(_settingsService.GetIconsLocations() ?? new List<string>());
        ApplicationsFolders = new ObservableCollection<string>(_settingsService.GetApplicationsLocations() ?? new List<string>());
        FoldersLocations = new ObservableCollection<string>(_settingsService.GetFoldersLocations() ?? new List<string>());

        AddIconFolderCommand = new RelayCommand(_ => AddIconFolder());
        RemoveIconFolderCommand = new RelayCommand(_ => RemoveIconFolder(), _ => SelectedIconFolder != null);

        AddApplicationFolderCommand = new RelayCommand(_ => AddApplicationFolder());
        RemoveApplicationFolderCommand = new RelayCommand(_ => RemoveApplicationFolder(), _ => SelectedApplicationFolder != null);
        AddFolderLocationCommand = new RelayCommand(_ => AddFolderLocation());
        RemoveFolderLocationCommand = new RelayCommand(_ => RemoveFolderLocation(), _ => SelectedFolderLocation != null);

        SaveCommand = new RelayCommand(_ => Save());
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
    }

    public ObservableCollection<string> IconsFolders { get; }
    public ObservableCollection<string> ApplicationsFolders { get; }
    public ObservableCollection<string> FoldersLocations { get; }

    public string? SelectedIconFolder { get; set; }
    public string? SelectedApplicationFolder { get; set; }
    public string? SelectedFolderLocation { get; set; }

    public RelayCommand AddIconFolderCommand { get; }
    public RelayCommand RemoveIconFolderCommand { get; }
    public RelayCommand AddApplicationFolderCommand { get; }
    public RelayCommand RemoveApplicationFolderCommand { get; }
    public RelayCommand AddFolderLocationCommand { get; }
    public RelayCommand RemoveFolderLocationCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CloseCommand { get; }

    public Action? CloseAction { get; set; }
    public Action? LocationsChanged { get; set; }

    private void AddIconFolder()
    {
        var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
        if (dlg.ShowDialog() != CommonFileDialogResult.Ok) return;
        IconsFolders.Add(dlg.FileName);
        LocationsChanged?.Invoke();
    }

    private void RemoveIconFolder()
    {
        if (SelectedIconFolder != null) IconsFolders.Remove(SelectedIconFolder);
        LocationsChanged?.Invoke();
    }

    private void AddApplicationFolder()
    {
        var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
        if (dlg.ShowDialog() != CommonFileDialogResult.Ok) return;
        ApplicationsFolders.Add(dlg.FileName);
        LocationsChanged?.Invoke();
    }

    private void AddFolderLocation()
    {
        var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
        if (dlg.ShowDialog() != CommonFileDialogResult.Ok) return;
        FoldersLocations.Add(dlg.FileName);
        LocationsChanged?.Invoke();
    }

    private void RemoveApplicationFolder()
    {
        if (SelectedApplicationFolder != null) ApplicationsFolders.Remove(SelectedApplicationFolder);
        LocationsChanged?.Invoke();
    }

    private void RemoveFolderLocation()
    {
        if (SelectedFolderLocation != null) FoldersLocations.Remove(SelectedFolderLocation);
        LocationsChanged?.Invoke();
    }

    private void Save()
    {
        _settingsService.SaveIconsLocations(IconsFolders.ToList());
        _settingsService.SaveApplicationsLocations(ApplicationsFolders.ToList());
        _settingsService.SaveFoldersLocations(FoldersLocations.ToList());

        try
        {
            LocationsChanged?.Invoke();
        }
        catch
        {
            // ignore
        }

        try
        {
            Services.SettingsService.TriggerLocationsChanged();
        }
        catch
        {
            // ignore
        }

        CloseAction?.Invoke();
    }
}
