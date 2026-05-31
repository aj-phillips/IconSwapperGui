using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.UI.Services.IconTools;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IconSwapperGui.UI.ViewModels;

public partial class FolderThemeDesignerViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogService;
    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;
    private readonly IconToolExportService _exportService;

    [ObservableProperty] private string _folderColorName = "Windows Yellow";
    [ObservableProperty] private string _accentColorName = "Indigo";
    [ObservableProperty] private string _folderStyleName = "Windows";
    [ObservableProperty] private string _tabStyleName = "Angled";
    [ObservableProperty] private string _labelPlacementName = "Front";
    [ObservableProperty] private bool _isBadgeEnabled = true;
    [ObservableProperty] private bool _showDocument = true;
    [ObservableProperty] private string _badge = "★";
    [ObservableProperty] private string _label = "Work";
    [ObservableProperty] private double _labelFontSize = 24;
    [ObservableProperty] private double _folderDepth = 0.65;
    [ObservableProperty] private double _cornerSoftness = 10;
    [ObservableProperty] private string _fileName = "CustomFolder";
    [ObservableProperty] private string _outputFolder = string.Empty;
    [ObservableProperty] private BitmapSource? _previewImage;

    public FolderThemeDesignerViewModel(
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
        UpdatePreview();
    }

    public IReadOnlyList<string> FolderColors { get; } = ["Windows Yellow", "Blue", "Green", "Purple", "Pink", "Amber", "Slate"];
    public IReadOnlyList<string> AccentColors { get; } = ["Indigo", "Emerald", "Rose", "Amber", "Cyan", "White"];
    public IReadOnlyList<string> FolderStyles { get; } = ["Windows", "Glass", "Project", "Archive"];
    public IReadOnlyList<string> TabStyles { get; } = ["Angled", "Rounded"];
    public IReadOnlyList<string> LabelPlacements { get; } = ["Front", "Tab", "None"];
    public string OutputPath => string.IsNullOrWhiteSpace(OutputFolder) ? string.Empty : Path.Combine(OutputFolder, $"{GetSafeFileName(FileName)}.ico");

    [RelayCommand]
    private async Task ChooseOutputFolderAsync()
    {
        var selected = _fileDialogService.BrowseForFolder(OutputFolder, "Select folder theme output folder");
        if (string.IsNullOrWhiteSpace(selected))
            return;

        OutputFolder = selected;
        _settingsService.Settings.Application.ExportLocation = selected;
        await _settingsService.SaveSettingsAsync();
        OnPropertyChanged(nameof(OutputPath));
    }

    private bool CanExport() => !string.IsNullOrWhiteSpace(OutputFolder) && !string.IsNullOrWhiteSpace(FileName);

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export()
    {
        try
        {
            _exportService.ExportFolderTheme(OutputPath, CreateRenderOptions());
            _notificationService.AddNotification("Folder Theme Designer", $"Exported {Path.GetFileName(OutputPath)}.", NotificationType.Success);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to export folder theme", ex);
            _notificationService.AddNotification("Folder Theme Designer", "Failed to export folder theme.", NotificationType.Error);
        }
    }

    partial void OnFolderColorNameChanged(string value) => UpdatePreview();
    partial void OnAccentColorNameChanged(string value) => UpdatePreview();
    partial void OnFolderStyleNameChanged(string value) => UpdatePreview();
    partial void OnTabStyleNameChanged(string value) => UpdatePreview();
    partial void OnLabelPlacementNameChanged(string value) => UpdatePreview();
    partial void OnIsBadgeEnabledChanged(bool value) => UpdatePreview();
    partial void OnShowDocumentChanged(bool value) => UpdatePreview();
    partial void OnBadgeChanged(string value) => UpdatePreview();
    partial void OnLabelChanged(string value) => UpdatePreview();
    partial void OnLabelFontSizeChanged(double value) => UpdatePreview();
    partial void OnFolderDepthChanged(double value) => UpdatePreview();
    partial void OnCornerSoftnessChanged(double value) => UpdatePreview();
    partial void OnFileNameChanged(string value)
    {
        OnPropertyChanged(nameof(OutputPath));
        ExportCommand.NotifyCanExecuteChanged();
    }
    partial void OnOutputFolderChanged(string value)
    {
        OnPropertyChanged(nameof(OutputPath));
        ExportCommand.NotifyCanExecuteChanged();
    }

    private void UpdatePreview()
    {
        PreviewImage = _exportService.RenderFolderThemeBitmap(256, CreateRenderOptions());
    }

    private FolderThemeRenderOptions CreateRenderOptions() => new()
    {
        FolderColor = GetColor(FolderColorName),
        AccentColor = GetColor(AccentColorName),
        Badge = Badge,
        Label = Label,
        ShowBadge = IsBadgeEnabled,
        ShowDocument = ShowDocument,
        StyleName = FolderStyleName,
        TabStyleName = TabStyleName,
        LabelPlacementName = LabelPlacementName,
        LabelFontSize = LabelFontSize,
        Depth = FolderDepth,
        CornerSoftness = CornerSoftness
    };

    private static Color GetColor(string name) => name switch
    {
        "Windows Yellow" => Color.FromRgb(255, 190, 45),
        "Green" or "Emerald" => Color.FromRgb(34, 197, 94),
        "Purple" or "Indigo" => Color.FromRgb(99, 102, 241),
        "Pink" or "Rose" => Color.FromRgb(244, 114, 182),
        "Amber" => Color.FromRgb(245, 158, 11),
        "Slate" => Color.FromRgb(100, 116, 139),
        "Cyan" => Color.FromRgb(6, 182, 212),
        "White" => Color.FromRgb(255, 255, 255),
        _ => Color.FromRgb(59, 130, 246)
    };

    private static string GetSafeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return string.IsNullOrWhiteSpace(name) ? "CustomFolder" : name.Trim();
    }
}
