using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Images;
using IconSwapperGui.Core.Models.Swapper;
using IconSwapperGui.UI.Services.IconTools;
using System.Collections.ObjectModel;
using System.IO;

namespace IconSwapperGui.UI.ViewModels;

public partial class IconNormalizerViewModel : ObservableObject
{
    private static readonly string[] SupportedExtensions = [".png", ".jpg", ".jpeg", ".ico"];

    private readonly IFileDialogService _fileDialogService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;
    private readonly IconToolExportService _exportService;

    [ObservableProperty] private ObservableCollection<Icon> _items = [];
    [ObservableProperty] private Icon? _selectedItem;
    [ObservableProperty] private string _outputFolder = string.Empty;
    [ObservableProperty] private int _outputSize = 256;
    [ObservableProperty] private ImageCropMode _cropMode = ImageCropMode.FitWithPadding;
    [ObservableProperty] private bool _useTransparentPadding = true;
    [ObservableProperty] private bool _exportPng = true;
    [ObservableProperty] private bool _exportIco = true;
    [ObservableProperty] private bool _isNormalizing;
    [ObservableProperty] private int _progress;

    public IconNormalizerViewModel(
        IFileDialogService fileDialogService,
        INotificationService notificationService,
        ISettingsService settingsService,
        ILoggingService loggingService,
        IconToolExportService exportService)
    {
        _fileDialogService = fileDialogService;
        _notificationService = notificationService;
        _settingsService = settingsService;
        _loggingService = loggingService;
        _exportService = exportService;
        OutputFolder = _settingsService.Settings.Application.ExportLocation;
    }

    private bool CanNormalize() => !IsNormalizing && Items.Count > 0 && !string.IsNullOrWhiteSpace(OutputFolder) && (ExportPng || ExportIco);

    [RelayCommand]
    private void AddImages()
    {
        AddPaths(_fileDialogService.OpenFiles("Select icons or images to normalize",
            "Images and icons (*.png;*.jpg;*.jpeg;*.ico)|*.png;*.jpg;*.jpeg;*.ico", true));
    }

    [RelayCommand]
    private async Task ChooseOutputFolderAsync()
    {
        var selected = _fileDialogService.BrowseForFolder(OutputFolder, "Select normalizer output folder");
        if (string.IsNullOrWhiteSpace(selected))
            return;

        OutputFolder = selected;
        _settingsService.Settings.Application.ExportLocation = selected;
        await _settingsService.SaveSettingsAsync();
    }

    [RelayCommand]
    private void RemoveSelected()
    {
        if (SelectedItem is null)
            return;

        Items.Remove(SelectedItem);
        SelectedItem = null;
        NormalizeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void Clear()
    {
        Items.Clear();
        SelectedItem = null;
        NormalizeCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanNormalize))]
    private async Task NormalizeAsync()
    {
        try
        {
            IsNormalizing = true;
            Progress = 0;

            var options = new ImageTransformOptions
            {
                CropMode = CropMode,
                UseTransparentPadding = UseTransparentPadding
            };

            var total = Items.Count;
            var completed = 0;

            foreach (var item in Items.ToList())
            {
                try
                {
                    await Task.Run(() => _exportService.NormalizeImage(item.Path, OutputFolder, OutputSize, options, ExportPng, ExportIco));
                }
                catch (Exception ex)
                {
                    _loggingService.LogError($"Failed to normalize {item.Path}", ex);
                    _notificationService.AddNotification("Icon Normalizer", $"Failed to normalize {item.Name}.", NotificationType.Error);
                }

                completed++;
                Progress = total == 0 ? 0 : (int)(completed * 100.0 / total);
            }

            _notificationService.AddNotification("Icon Normalizer", $"Normalized {completed} item{(completed == 1 ? string.Empty : "s")}.", NotificationType.Success);
        }
        finally
        {
            IsNormalizing = false;
            Progress = 0;
        }
    }

    public void AddPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths.SelectMany(ExpandPath))
        {
            if (!File.Exists(path) || !IsSupported(path))
                continue;

            if (Items.Any(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase)))
                continue;

            Items.Add(new Icon(Path.GetFileNameWithoutExtension(path), path));
        }

        NormalizeCommand.NotifyCanExecuteChanged();
    }

    partial void OnOutputFolderChanged(string value) => NormalizeCommand.NotifyCanExecuteChanged();
    partial void OnExportPngChanged(bool value) => NormalizeCommand.NotifyCanExecuteChanged();
    partial void OnExportIcoChanged(bool value) => NormalizeCommand.NotifyCanExecuteChanged();
    partial void OnIsNormalizingChanged(bool value) => NormalizeCommand.NotifyCanExecuteChanged();

    private static IEnumerable<string> ExpandPath(string path)
    {
        if (Directory.Exists(path))
            return Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly).Where(IsSupported);

        return [path];
    }

    private static bool IsSupported(string path)
        => SupportedExtensions.Any(ext => string.Equals(Path.GetExtension(path), ext, StringComparison.OrdinalIgnoreCase));
}
