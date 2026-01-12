using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models.Swapper;
using IconSwapperGui.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.UI.ViewModels;

public partial class ConverterViewModel : ObservableObject
{
    // Services
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly IIconCreatorService _iconCreatorService;
    private readonly IIconManagementService _iconManagementService;
    private List<IFileSystemWatcherService>? _fileSystemWatcherServices;
    private readonly ILoggingService _loggingService;

    // Properties
    private CancellationTokenSource? _convertCts;

    // Observable Properties
    [ObservableProperty] private List<string> _iconsFolderPaths;
    [ObservableProperty] private bool _canConvertImages;
    [ObservableProperty] private ObservableCollection<Icon> _filteredIcons;
    [ObservableProperty] private ObservableCollection<Icon> _icons;
    [ObservableProperty] private bool _canDeleteImagesAfterConversion;
    [ObservableProperty] private string? _filterString;
    [ObservableProperty] private bool _isConverting;
    [ObservableProperty] private int _conversionProgress;

    public ConverterViewModel(
        ISettingsService settingsService,
        INotificationService notificationService,
        IIconCreatorService iconCreatorService,
        IIconManagementService iconManagementService,
        ILoggingService loggingService)
    {
        _settingsService = settingsService;
        _notificationService = notificationService;
        _iconCreatorService = iconCreatorService;
        _iconManagementService = iconManagementService;
        _loggingService = loggingService;

        _loggingService.LogInfo("ConverterViewModel initializing.");

        _iconsFolderPaths = new List<string>();
        Icons = new ObservableCollection<Icon>();
        FilteredIcons = new ObservableCollection<Icon>();

        Initialize();

        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FilterString))
                FilterIcons();
        };
    }

    private static bool IsOnUiThread()
        => Application.Current?.Dispatcher?.CheckAccess() ?? true;

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    private static Task RunOnUiThreadAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
    }

    public void Initialize()
    {
        try
        {
            IconsFolderPaths = _settingsService.Settings.Application.ConverterIconsLocations;

            RunOnUiThread(() =>
            {
                PopulateIconsList();
                ValidateAndSetupFileSystemWatcher();
            });
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error initializing ConverterViewModel settings", ex);
        }
    }

    private bool CanExecuteConvertIcon()
    {
        var firstPath = IconsFolderPaths.FirstOrDefault();
        return !IsConverting && !string.IsNullOrEmpty(firstPath) && Icons.Any();
    }

    [RelayCommand(CanExecute = nameof(CanExecuteConvertIcon))]
    private async Task ConvertIconAsync()
    {
        var iconSizes = new[] { 16, 32, 48, 64, 128, 256 };
        var supportedExtensions = new[] { "*.png", "*.jpg", "*.jpeg" };

        if (IsConverting) return;

        _convertCts?.Cancel();
        _convertCts = new CancellationTokenSource();
        var ct = _convertCts.Token;

        try
        {
            IsConverting = true;
            ConversionProgress = 0;

            if (!IconsFolderPaths.Any() || !Directory.Exists(IconsFolderPaths.First())) return;

            foreach (var folder in IconsFolderPaths)
            {
                var directoryInfo = new DirectoryInfo(folder);
                var files = supportedExtensions
                    .SelectMany(ext => directoryInfo.GetFiles(ext, SearchOption.TopDirectoryOnly)).ToList();
                var total = files.Count;
                var processed = 0;

                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        var sourceImagePath = file.FullName;
                        var targetIconPath = Path.ChangeExtension(sourceImagePath, ".ico");

                        if (File.Exists(targetIconPath)) File.Delete(targetIconPath);

                        await Task.Run(
                                () => _iconCreatorService.CreateMultiSizeIcoFromImage(sourceImagePath, targetIconPath,
                                    iconSizes),
                                ct)
                            .ConfigureAwait(true);

                        if (CanDeleteImagesAfterConversion)
                        {
                            await RunOnUiThreadAsync(() =>
                            {
                                var existing = Icons.FirstOrDefault(i => i.Path == file.FullName);
                                if (existing != null)
                                    Icons.Remove(existing);
                            }).ConfigureAwait(false);

                            file.Delete();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _loggingService.LogInfo("Conversion canceled by user");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _loggingService.LogError($"Failed to convert image to icon: {file.Name}", ex);
                        await RunOnUiThreadAsync(() =>
                            MessageBox.Show("Conversion Error",
                                $"Failed to convert image '{file.Name}' to icon.\n\nError: {ex.Message}",
                                MessageBoxButton.OK, MessageBoxImage.Error)).ConfigureAwait(false);
                    }

                    processed++;

                    var progress = total == 0 ? 0 : (int)(processed * 100.0 / total);
                    await RunOnUiThreadAsync(() => ConversionProgress = progress).ConfigureAwait(false);
                }

                if (!ct.IsCancellationRequested)
                {
                    await RunOnUiThreadAsync(() => _notificationService.AddNotification("Converter",
                        "Successfully converted images to ICOs!", NotificationType.Success));
                    RunOnUiThread(RefreshGui);
                }
            }
        }
        finally
        {
            IsConverting = false;
            ConversionProgress = 0;
        }
    }

    public void PopulateIconsList()
    {
        _loggingService.LogInfo($"PopulateIconsList called");

        if (!IconsFolderPaths.Any())
        {
            _loggingService.LogWarning("Folder path is null or empty, cannot populate icons list");
            return;
        }

        if (!Directory.Exists(IconsFolderPaths.First()))
        {
            _loggingService.LogWarning($"Folder path does not exist: {IconsFolderPaths.First()}");
            return;
        }

        try
        {
            IEnumerable<Icon> newIcons = new List<Icon>();

            newIcons = IconsFolderPaths.Aggregate(newIcons,
                (current, folder) => current.Concat(_iconManagementService.GetIcons(folder)));

            Icons = new ObservableCollection<Icon>(newIcons);

            _loggingService.LogInfo($"Populated icons list with {Icons.Count} icons");

            FilterIcons();
            UpdateConvertButtonEnabledState();
        }
        catch (Exception ex)
        {
            _loggingService.LogError($"Error populating icons list from folder paths", ex);
        }
    }

    private void ValidateAndSetupFileSystemWatcher()
    {
        var folders = IconsFolderPaths
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (folders.Count > 0 && folders.All(Directory.Exists))
        {
            try
            {
                _fileSystemWatcherServices = new List<IFileSystemWatcherService>();

                foreach (var folder in folders)
                {
                    _loggingService.LogInfo($"Validating and setting up file system watcher for: {folder ?? "null"}");

                    var watcherService =
                        new FileSystemWatcherService(folder, OnIconsDirectoryChanged, OnIconsDirectoryRenamed);

                    _fileSystemWatcherServices.Add(watcherService);

                    watcherService.StartWatching();

                    _loggingService.LogInfo($"File system watcher set up successfully for: {folder ?? "null"}");
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError("Error setting up file system watcher", ex);
            }
        }
        else
        {
            _loggingService.LogWarning("File system watcher not set up - invalid path");
        }
    }

    partial void OnIconsChanged(ObservableCollection<Icon> value)
    {
        _loggingService.LogInfo($"Icons collection changed, new count: {value?.Count ?? 0}");

        FilterIcons();

        ConvertIconCommand?.NotifyCanExecuteChanged();
    }

    partial void OnIsConvertingChanged(bool value)
    {
        ConvertIconCommand?.NotifyCanExecuteChanged();
    }

    private void OnIconsDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        _loggingService.LogInfo($"Icons directory changed event fired for: {e.ChangeType} - {e.FullPath}");

        RunOnUiThread(PopulateIconsList);
    }

    private void OnIconsDirectoryRenamed(object sender, RenamedEventArgs e)
    {
        _loggingService.LogInfo($"Icons directory renamed event fired from {e.OldFullPath} to {e.FullPath}");

        RunOnUiThread(PopulateIconsList);
    }

    private void FilterIcons()
    {
        try
        {
            FilteredIcons = _iconManagementService.FilterIcons(Icons, FilterString);

            _loggingService.LogInfo(
                $"Filtered icons: {FilteredIcons.Count} of {Icons.Count} icons with filter: {FilterString ?? "null"}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error filtering icons with filter string: {FilterString ?? \"null\"}", ex);
        }
    }

    public void RefreshGui()
    {
        _loggingService.LogInfo("Refreshing GUI, clearing icons and repopulating");

        try
        {
            RunOnUiThread(() => Icons.Clear());
            RunOnUiThread(PopulateIconsList);
            _loggingService.LogInfo("GUI refresh completed successfully");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error refreshing GUI", ex);
        }
    }

    private void UpdateConvertButtonEnabledState()
    {
        var previousState = CanConvertImages;

        CanConvertImages = Icons.Count > 0;

        if (previousState != CanConvertImages)
        {
            _loggingService.LogInfo(
                $"Convert button enabled state changed to: {CanConvertImages} (Icons count: {Icons.Count})");
        }
    }
}