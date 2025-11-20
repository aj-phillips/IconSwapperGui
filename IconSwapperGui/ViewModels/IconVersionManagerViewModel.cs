using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IconSwapperGui.Commands;
using IconSwapperGui.Models;
using IconSwapperGui.Services.Interfaces;
using Serilog;

namespace IconSwapperGui.ViewModels;

public class IconVersionManagerViewModel : INotifyPropertyChanged
{
    private readonly IIconHistoryService _historyService;
    private readonly IDialogService _dialogService;
    private IconHistory _history;
    private IconVersion _selectedVersion;
    private string _filePath;
    private string _previewImagePath;
    private bool _isLoading;
    private readonly ILogger _logger = Log.ForContext<IconVersionManagerViewModel>();

    public IconVersionManagerViewModel(
        IIconHistoryService historyService,
        IDialogService dialogService,
        string filePath)
    {
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

        Versions = new ObservableCollection<IconVersion>();

        Versions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Versions));
            OnPropertyChanged(nameof(TotalVersions));
            OnPropertyChanged(nameof(StatusMessage));
        };
        
        RevertCommand = new RelayCommand(async _ => await RevertToSelectedVersionAsync(), _ => CanRevert());
        DeleteVersionCommand = new RelayCommand(async _ => await DeleteSelectedVersionAsync(), _ => CanDeleteVersion());
        ClearHistoryCommand = new RelayCommand(async _ => await ClearHistoryAsync(), _ => Versions.Any());
        RefreshCommand = new RelayCommand(async _ => await LoadHistoryAsync());
        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
        
        _ = LoadHistoryAsync();
    }

    public ObservableCollection<IconVersion> Versions { get; }

    public string FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FileName));
        }
    }

    public string FileName => System.IO.Path.GetFileName(FilePath);

    public IconVersion SelectedVersion
    {
        get => _selectedVersion;
        set
        {
            _selectedVersion = value;
            OnPropertyChanged();
            UpdatePreview();
        }
    }

    public string PreviewImagePath
    {
        get => _previewImagePath;
        set
        {
            _previewImagePath = value;
            OnPropertyChanged();
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public int TotalVersions => Versions.Count;

    public string StatusMessage => Versions.Any()
        ? $"{TotalVersions} version{(TotalVersions != 1 ? "s" : "")} found"
        : "No history available";

    public ICommand RevertCommand { get; }
    public ICommand DeleteVersionCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CloseCommand { get; }

    public event Action RequestClose;
    public event Action<IconVersion> VersionReverted;

    private async Task LoadHistoryAsync()
    {
        try
        {
            IsLoading = true;
            _logger.Information("Loading history for {FilePath}", FilePath);

            _history = await _historyService.GetHistoryAsync(FilePath);

            Versions.Clear();

            if (_history != null)
            {
                foreach (var version in _history.Versions.OrderByDescending(v => v.Timestamp))
                {
                    Versions.Add(version);
                }
                
                SelectedVersion = Versions.FirstOrDefault(v => v.IsCurrent);

                _logger.Information("Loaded {Count} versions", Versions.Count);
            }
            else
            {
                _logger.Information("No history found for {FilePath}", FilePath);
            }

            OnPropertyChanged(nameof(TotalVersions));
            OnPropertyChanged(nameof(StatusMessage));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load history");
            _dialogService.ShowError("Error", "Failed to load version history");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdatePreview()
    {
        if (SelectedVersion != null)
        {
            PreviewImagePath = SelectedVersion.ThumbnailPath ?? SelectedVersion.IconPath;
            _logger.Debug("Preview updated to {Path}", PreviewImagePath);
        }
        else
        {
            PreviewImagePath = null;
        }
    }

    private bool CanRevert()
    {
        return SelectedVersion != null && !SelectedVersion.IsCurrent;
    }

    private async Task RevertToSelectedVersionAsync()
    {
        if (SelectedVersion == null) return;

        try
        {
            var confirm = _dialogService.ShowConfirmation(
                "Revert Icon",
                $"Are you sure you want to revert to the icon from {SelectedVersion.TimestampDisplay}?");

            if (!confirm) return;

            _logger.Information("Reverting to version {VersionId}", SelectedVersion.Id);

            var success = await _historyService.RevertToVersionAsync(FilePath, SelectedVersion.Id);

            if (success)
            {
                _dialogService.ShowInformation("Success", "Icon reverted successfully!");
                VersionReverted?.Invoke(SelectedVersion);
                
                await LoadHistoryAsync();
            }
            else
            {
                _dialogService.ShowError("Error", "Failed to revert icon");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error reverting to version");
            _dialogService.ShowError("Error", $"Failed to revert: {ex.Message}");
        }
    }

    private bool CanDeleteVersion()
    {
        return SelectedVersion != null && !SelectedVersion.IsCurrent && Versions.Count > 1;
    }

    private async Task DeleteSelectedVersionAsync()
    {
        if (SelectedVersion == null) return;

        try
        {
            var confirm = _dialogService.ShowConfirmation(
                "Delete Version",
                $"Are you sure you want to delete this version from {SelectedVersion.TimestampDisplay}?\n\nThis cannot be undone.");

            if (!confirm) return;

            _logger.Information("Deleting version {VersionId}", SelectedVersion.Id);

            var success = await _historyService.DeleteVersionAsync(FilePath, SelectedVersion.Id);

            if (success)
            {
                await LoadHistoryAsync();
            }
            else
            {
                _dialogService.ShowError("Error", "Failed to delete version");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error deleting version");
            _dialogService.ShowError("Error", $"Failed to delete version: {ex.Message}");
        }
    }

    private async Task ClearHistoryAsync()
    {
        try
        {
            var confirm = _dialogService.ShowConfirmation(
                "Clear History",
                $"Are you sure you want to clear ALL version history for {FileName}?\n\nThis cannot be undone.");

            if (!confirm) return;

            _logger.Information("Clearing all history for {FilePath}", FilePath);

            var success = await _historyService.ClearHistoryAsync(FilePath);

            if (success)
            {
                _dialogService.ShowInformation("Success", "History cleared successfully!");
                await LoadHistoryAsync();
            }
            else
            {
                _dialogService.ShowError("Error", "Failed to clear history");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error clearing history");
            _dialogService.ShowError("Error", $"Failed to clear history: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}