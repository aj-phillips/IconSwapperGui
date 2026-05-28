using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Images;
using IconSwapperGui.Core.Models.Swapper;
using IconSwapperGui.Infrastructure.Services;
using IconSwapperGui.UI.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace IconSwapperGui.UI.ViewModels;

public partial class ConverterViewModel : ObservableObject
{
    private static readonly string[] SupportedImageExtensions = [".png", ".jpg", ".jpeg"];

    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly IIconCreatorService _iconCreatorService;
    private readonly IIconManagementService _iconManagementService;
    private List<IFileSystemWatcherService>? _fileSystemWatcherServices;
    private readonly ILoggingService _loggingService;
    private readonly IDialogService _dialogService;
    private readonly IDispatcher _dispatcher;
    private readonly IFileDialogService _fileDialogService;
    private readonly IWindowService _windowService;

    private CancellationTokenSource? _convertCts;

    [ObservableProperty] private List<string> _iconsFolderPaths;
    [ObservableProperty] private bool _canConvertImages;
    [ObservableProperty] private ObservableCollection<Icon> _filteredIcons;
    [ObservableProperty] private ObservableCollection<Icon> _icons;
    [ObservableProperty] private bool _canDeleteImagesAfterConversion;
    [ObservableProperty] private string? _filterString;
    [ObservableProperty] private bool _isConverting;
    [ObservableProperty] private int _conversionProgress;
    [ObservableProperty] private bool _useSourceFolderForConversion = true;
    [ObservableProperty] private string _conversionOutputFolder = string.Empty;
    [ObservableProperty] private ImageCropMode _selectedCropMode = ImageCropMode.CenterCrop;
    [ObservableProperty] private Icon? _selectedImage;

    public ConverterViewModel(
        ISettingsService settingsService,
        INotificationService notificationService,
        IIconCreatorService iconCreatorService,
        IIconManagementService iconManagementService,
        ILoggingService loggingService,
        IDialogService dialogService,
        IDispatcher dispatcher,
        IFileDialogService fileDialogService,
        IWindowService windowService)
    {
        _settingsService = settingsService;
        _notificationService = notificationService;
        _iconCreatorService = iconCreatorService;
        _iconManagementService = iconManagementService;
        _loggingService = loggingService;
        _dialogService = dialogService;
        _dispatcher = dispatcher;
        _fileDialogService = fileDialogService;
        _windowService = windowService;

        _loggingService.LogInfo("ConverterViewModel initializing.");

        _iconsFolderPaths = new List<string>();
        Icons = new ObservableCollection<Icon>();
        FilteredIcons = new ObservableCollection<Icon>();
        ConversionOutputFolder = _settingsService.Settings.Application.ConverterOutputLocation;
        UseSourceFolderForConversion = string.IsNullOrWhiteSpace(ConversionOutputFolder);

        Initialize();

        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FilterString))
                FilterIcons();
        };
    }

    private void RunOnUiThread(Action action)
    {
        _dispatcher.Invoke(action);
    }

    private Task RunOnUiThreadAsync(Action action)
    {
        _dispatcher.Invoke(action);
        return Task.CompletedTask;
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

    private bool CanExecuteConvertIcon() => !IsConverting && Icons.Any();

    [RelayCommand(CanExecute = nameof(CanExecuteConvertIcon))]
    private async Task ConvertIconAsync()
    {
        var iconSizes = new[] { 16, 32, 48, 64, 128, 256 };

        if (IsConverting) return;

        _convertCts?.Cancel();
        _convertCts = new CancellationTokenSource();
        var ct = _convertCts.Token;

        try
        {
            IsConverting = true;
            ConversionProgress = 0;

            var files = Icons
                .Where(icon => IsSupportedImagePath(icon.Path) && File.Exists(icon.Path))
                .Select(icon => new FileInfo(icon.Path))
                .DistinctBy(file => file.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var total = files.Count;
            var processed = 0;
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var queuedImage = Icons.FirstOrDefault(icon =>
                        string.Equals(icon.Path, file.FullName, StringComparison.OrdinalIgnoreCase));
                    var options = queuedImage is ConverterImage converterImage
                        ? converterImage.TransformOptions
                        : new ImageTransformOptions { CropMode = SelectedCropMode };

                    var targetFolder = UseSourceFolderForConversion || string.IsNullOrWhiteSpace(ConversionOutputFolder)
                        ? file.DirectoryName
                        : ConversionOutputFolder;

                    if (string.IsNullOrWhiteSpace(targetFolder))
                        continue;

                    Directory.CreateDirectory(targetFolder);
                    var targetIconPath = Path.Combine(targetFolder, Path.GetFileNameWithoutExtension(file.Name) + ".ico");

                    if (File.Exists(targetIconPath)) File.Delete(targetIconPath);

                    await Task.Run(
                            () => _iconCreatorService.CreateMultiSizeIcoFromImage(file.FullName, targetIconPath,
                                iconSizes, options),
                            ct)
                        .ConfigureAwait(true);

                    if (CanDeleteImagesAfterConversion)
                    {
                        await RunOnUiThreadAsync(() =>
                        {
                            var existing = Icons.FirstOrDefault(i =>
                                string.Equals(i.Path, file.FullName, StringComparison.OrdinalIgnoreCase));
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
                        _dialogService.ShowError(
                            "Conversion Error",
                            $"Failed to convert image '{file.Name}' to icon.\n\nError: {ex.Message}"))
                        .ConfigureAwait(false);
                }

                processed++;
                var progress = total == 0 ? 0 : (int)(processed * 100.0 / total);
                await RunOnUiThreadAsync(() => ConversionProgress = progress).ConfigureAwait(false);
            }

            if (!ct.IsCancellationRequested)
            {
                await RunOnUiThreadAsync(() => _notificationService.AddNotification("Converter",
                    $"Converted {processed} image{(processed == 1 ? string.Empty : "s")} to ICO.",
                    NotificationType.Success));
                RunOnUiThread(RefreshGui);
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
        _loggingService.LogInfo("PopulateIconsList called");

        try
        {
            IEnumerable<Icon> newIcons = Icons.Where(icon => icon is ConverterImage { IsManual: true }).ToList();

            foreach (var folder in IconsFolderPaths.Where(Directory.Exists))
            {
                newIcons = newIcons.Concat(_iconManagementService.GetIcons(folder, SupportedImageExtensions.ToList())
                    .Select(icon => new ConverterImage(icon.Name, icon.Path)));
            }

            Icons = new ObservableCollection<Icon>(
                newIcons.DistinctBy(icon => icon.Path, StringComparer.OrdinalIgnoreCase));

            _loggingService.LogInfo($"Populated converter queue with {Icons.Count} images");
            FilterIcons();
            UpdateConvertButtonEnabledState();
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error populating converter image list", ex);
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
                foreach (var svc in _fileSystemWatcherServices ?? [])
                    svc.Dispose();

                _fileSystemWatcherServices = new List<IFileSystemWatcherService>();

                foreach (var folder in folders)
                {
                    var watcherService =
                        new FileSystemWatcherService(folder, OnIconsDirectoryChanged, OnIconsDirectoryRenamed);

                    _fileSystemWatcherServices.Add(watcherService);
                    watcherService.StartWatching();
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
        FilterIcons();
        ConvertIconCommand?.NotifyCanExecuteChanged();
    }

    partial void OnIsConvertingChanged(bool value)
    {
        ConvertIconCommand?.NotifyCanExecuteChanged();
    }

    partial void OnConversionOutputFolderChanged(string value)
    {
        _settingsService.Settings.Application.ConverterOutputLocation = value;
        ConvertIconCommand?.NotifyCanExecuteChanged();
    }

    partial void OnSelectedImageChanged(Icon? value)
    {
        EditImageCropCommand?.NotifyCanExecuteChanged();
        RemoveConverterImageCommand?.NotifyCanExecuteChanged();
    }

    private void OnIconsDirectoryChanged(object sender, FileSystemEventArgs e)
    {
        RunOnUiThread(PopulateIconsList);
    }

    private void OnIconsDirectoryRenamed(object sender, RenamedEventArgs e)
    {
        RunOnUiThread(PopulateIconsList);
    }

    private void FilterIcons()
    {
        try
        {
            FilteredIcons = _iconManagementService.FilterIcons(Icons, FilterString);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error filtering converter images", ex);
        }
    }

    public void RefreshGui()
    {
        try
        {
            RunOnUiThread(PopulateIconsList);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Error refreshing converter GUI", ex);
        }
    }

    private void UpdateConvertButtonEnabledState()
    {
        CanConvertImages = Icons.Count > 0;
        ConvertIconCommand?.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task AddConverterLocationAsync()
    {
        var selected = _fileDialogService.BrowseForFolder(IconsFolderPaths.FirstOrDefault(), "Select converter image folder");
        if (TryAddLocation(_settingsService.Settings.Application.ConverterIconsLocations, selected))
        {
            await _settingsService.SaveSettingsAsync();
            IconsFolderPaths = _settingsService.Settings.Application.ConverterIconsLocations;
            RunOnUiThread(() =>
            {
                PopulateIconsList();
                ValidateAndSetupFileSystemWatcher();
            });
        }
    }

    [RelayCommand]
    private Task AddConverterImagesAsync()
    {
        var selected = _fileDialogService.OpenFiles(
            "Select images to convert",
            "Images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            true);

        AddManualImages(selected);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ChooseConversionOutputFolderAsync()
    {
        var selected = _fileDialogService.BrowseForFolder(ConversionOutputFolder, "Select converter output folder");
        if (string.IsNullOrWhiteSpace(selected))
            return;

        ConversionOutputFolder = selected;
        UseSourceFolderForConversion = false;
        await _settingsService.SaveSettingsAsync();
    }

    [RelayCommand]
    private async Task ClearConversionOutputFolderAsync()
    {
        ConversionOutputFolder = string.Empty;
        UseSourceFolderForConversion = true;
        await _settingsService.SaveSettingsAsync();
    }

    [RelayCommand]
    private void RemoveConverterImage(object parameter)
    {
        if (parameter is not Icon icon)
            return;

        Icons.Remove(icon);
        if (SelectedImage == icon)
            SelectedImage = null;

        FilterIcons();
        UpdateConvertButtonEnabledState();
    }

    private bool CanEditImageCrop() => SelectedImage is not null && File.Exists(SelectedImage.Path);

    [RelayCommand(CanExecute = nameof(CanEditImageCrop))]
    private void EditImageCrop()
    {
        if (SelectedImage is null)
            return;

        var converterImage = EnsureConverterImage(SelectedImage);
        var viewModel = new ImageCropEditorViewModel(converterImage.Path, converterImage.TransformOptions);
        if (_windowService.ShowImageCropEditor(viewModel) == true)
        {
            converterImage.TransformOptions = viewModel.ToOptions();
            SelectedCropMode = converterImage.TransformOptions.CropMode;
            _notificationService.AddNotification("Converter", $"Updated crop settings for {converterImage.Name}.", NotificationType.Success);
        }
    }

    public async Task AddDroppedPathsAsync(IEnumerable<string> paths)
    {
        var changedLocations = false;
        var manualImages = new List<string>();

        foreach (var path in paths.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()))
        {
            if (Directory.Exists(path))
            {
                changedLocations |= TryAddLocation(_settingsService.Settings.Application.ConverterIconsLocations, path);
            }
            else if (File.Exists(path) && IsSupportedImagePath(path))
            {
                manualImages.Add(path);
            }
        }

        if (manualImages.Count > 0)
            AddManualImages(manualImages);

        if (changedLocations)
        {
            await _settingsService.SaveSettingsAsync();
            IconsFolderPaths = _settingsService.Settings.Application.ConverterIconsLocations;
            RunOnUiThread(() =>
            {
                PopulateIconsList();
                ValidateAndSetupFileSystemWatcher();
            });
        }
    }

    private void AddManualImages(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(IsSupportedImagePath))
        {
            if (!File.Exists(path))
                continue;

            if (Icons.Any(icon => string.Equals(icon.Path, path, StringComparison.OrdinalIgnoreCase)))
                continue;

            Icons.Add(new ConverterImage(Path.GetFileNameWithoutExtension(path), path, true));
        }

        FilterIcons();
        UpdateConvertButtonEnabledState();
    }

    private ConverterImage EnsureConverterImage(Icon icon)
    {
        if (icon is ConverterImage converterImage)
            return converterImage;

        converterImage = new ConverterImage(icon.Name, icon.Path);
        var index = Icons.IndexOf(icon);
        if (index >= 0)
            Icons[index] = converterImage;

        SelectedImage = converterImage;
        return converterImage;
    }

    private static bool IsSupportedImagePath(string path)
    {
        var extension = Path.GetExtension(path);
        return SupportedImageExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryAddLocation(ICollection<string> locations, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = path.Trim();
        if (locations.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase)))
            return false;

        locations.Add(path);
        return true;
    }
}
