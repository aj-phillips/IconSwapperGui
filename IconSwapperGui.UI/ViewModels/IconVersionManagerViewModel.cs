using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Swapper.IconVersionManagement;
using System.Collections.ObjectModel;
using System.Windows;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.UI.ViewModels;

public partial class IconVersionManagerViewModel : ObservableObject
{
    private readonly IIconHistoryService _iconHistoryService;
    private readonly INotificationService _notificationService;
    private readonly ILoggingService _loggingService;

    // Properties
    private IconHistory _iconHistory;
    public ObservableCollection<IconVersion> Versions { get; }

    // Observable Properties
    [ObservableProperty] private IconVersion _selectedVersion;
    [ObservableProperty] private string _filePath;
    [ObservableProperty] private string _previewImagePath;
    [ObservableProperty] private bool _isLoading;

    // Events
    public event Action RequestClose;
    public event Action<IconVersion> VersionReverted;

    // Set Properties
    public string FileName => System.IO.Path.GetFileName(FilePath);

    public int TotalVersions => Versions.Count;

    public string StatusMessage => Versions.Any()
        ? $"{TotalVersions} version{(TotalVersions != 1 ? "s" : "")} found"
        : "No history available";

    public IconVersionManagerViewModel(
        IIconHistoryService iconHistoryService,
        ILoggingService loggingService,
        INotificationService notificationService,
        string filePath)
    {
        _iconHistoryService = iconHistoryService;
        _notificationService = notificationService;
        _loggingService = loggingService;

        Versions = new ObservableCollection<IconVersion>();

        Versions.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Versions));
            OnPropertyChanged(nameof(TotalVersions));
            OnPropertyChanged(nameof(StatusMessage));

            DeleteSelectedVersionCommand.NotifyCanExecuteChanged();
            ClearHistoryCommand.NotifyCanExecuteChanged();
        };

        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

        _ = LoadHistoryAsync();
    }

    // Commands

    private bool CanRevert()
    {
        return SelectedVersion != null && !SelectedVersion.IsCurrent;
    }

    [RelayCommand(CanExecute = nameof(CanRevert))]
    private async Task RevertToSelectedVersionAsync()
    {
        if (SelectedVersion == null) return;

        try
        {
            var confirm = MessageBox.Show(
                "Revert Icon",
                $"Are you sure you want to revert to the icon from {SelectedVersion.RelativeTimeDisplay}?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;

            if (!confirm) return;

            _loggingService.LogInfo($"Reverting to version {SelectedVersion.Id}");

            var success = await _iconHistoryService.RevertToVersionAsync(FilePath, SelectedVersion.Id);

            if (success)
            {
                _notificationService.AddNotification("Icon Version Manager", "Icon reverted successfully!",
                    NotificationType.Success);

                VersionReverted?.Invoke(SelectedVersion);

                await LoadHistoryAsync();
            }
            else
            {
                _notificationService.AddNotification("Icon Version Manager", "Failed to revert icon",
                    NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error reverting to version", ex);
            _notificationService.AddNotification("Icon Version Manager", $"Failed to revert: {ex.Message}",
                NotificationType.Error);
        }
    }

    private bool CanDeleteVersion()
    {
        return SelectedVersion != null && !SelectedVersion.IsCurrent && Versions.Count > 1;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteVersion))]
    private async Task DeleteSelectedVersionAsync()
    {
        if (SelectedVersion == null) return;

        try
        {
            var confirm = MessageBox.Show(
                "Delete Version",
                $"Are you sure you want to delete this version from {SelectedVersion.RelativeTimeDisplay}?\n\nThis cannot be undone.",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

            if (!confirm) return;

            _loggingService.LogInfo($"Deleting version {SelectedVersion.Id}");

            var success = await _iconHistoryService.DeleteVersionAsync(FilePath, SelectedVersion.Id);

            if (success)
            {
                await LoadHistoryAsync();
            }
            else
            {
                _notificationService.AddNotification("Icon Version Manager", "Failed to delete version",
                    NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error deleting version", ex);
            _notificationService.AddNotification("Icon Version Manager", $"Failed to delete version: {ex.Message}",
                NotificationType.Error);
        }
    }

    private bool CanClearHistory() => Versions.Any();

    [RelayCommand(CanExecute = nameof(CanClearHistory))]
    private async Task ClearHistoryAsync()
    {
        try
        {
            var confirm = MessageBox.Show(
                "Clear History",
                $"Are you sure you want to clear ALL version history for {FileName}?\n\nThis cannot be undone.",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

            if (!confirm) return;

            _loggingService.LogInfo($"Clearing all history for {FilePath}");

            var success = await _iconHistoryService.ClearHistoryAsync(FilePath);

            if (success)
            {
                _notificationService.AddNotification("Icon Version Manager", "History cleared successfully!",
                    NotificationType.Success);
                await LoadHistoryAsync();
            }
            else
            {
                _notificationService.AddNotification("Icon Version Manager", "Failed to clear history",
                    NotificationType.Error);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error clearing history", ex);
            _notificationService.AddNotification("Icon Version Manager", $"Failed to clear history: {ex.Message}",
                NotificationType.Error);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        try
        {
            var folderPath = System.IO.Path.GetDirectoryName(SelectedVersion.IconPath);
            if (folderPath != null)
            {
                System.Diagnostics.Process.Start("explorer.exe", folderPath);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to open folder", ex);
            _notificationService.AddNotification("Icon Version Manager",
                $"Failed to open folder: {ex.Message}", NotificationType.Error);
        }
    }

    // Helpers

    private async Task LoadHistoryAsync()
    {
        try
        {
            IsLoading = true;
            _loggingService.LogInfo($"Loading history for {FilePath}");

            _iconHistory = await _iconHistoryService.GetHistoryAsync(FilePath);

            Versions.Clear();

            if (_iconHistory != null)
            {
                foreach (var version in _iconHistory.Versions.OrderByDescending(v => v.Timestamp))
                {
                    Versions.Add(version);
                }

                SelectedVersion = Versions.FirstOrDefault(v => v.IsCurrent);

                _loggingService.LogInfo($"Loaded {Versions.Count} versions");
            }
            else
            {
                _loggingService.LogInfo($"No history found for {FilePath}");
            }

            OnPropertyChanged(nameof(TotalVersions));
            OnPropertyChanged(nameof(StatusMessage));
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to load history", ex);
            _notificationService.AddNotification("Icon Version Manager",
                $"Failed to load version history: {ex.Message}", NotificationType.Error);
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
            PreviewImagePath = SelectedVersion.ThumbnailPath;
            _loggingService.LogInfo($"Preview updated to {PreviewImagePath}");
        }
        else
        {
            PreviewImagePath = null;
        }
    }

    partial void OnSelectedVersionChanged(IconVersion value)
    {
        UpdatePreview();
        RevertToSelectedVersionCommand.NotifyCanExecuteChanged();
        DeleteSelectedVersionCommand.NotifyCanExecuteChanged();
    }

    partial void OnFilePathChanged(string value)
    {
        OnPropertyChanged(nameof(FileName));
    }
}