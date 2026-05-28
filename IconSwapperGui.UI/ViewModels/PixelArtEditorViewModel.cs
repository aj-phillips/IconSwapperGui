using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.PixelArt;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IconSwapperGui.UI.Services.PixelArtEditor;
using System;

namespace IconSwapperGui.UI.ViewModels;

public partial class PixelArtEditorViewModel : ObservableObject
{
    private const int MaxImportDimension = 128;
    private const int MaxRecentColors = 12;
    private const int MaxHistoryEntries = 50;

    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;
    private readonly IFileDialogService _fileDialogService;
    private readonly PixelArtExportService _exportService;

    private uint[] _pixelsArgb = Array.Empty<uint>();
    private int _activeStrokeIndex = -1;
    private int _layerCounter = 1;
    private bool _strokeHasHistorySnapshot;
    private readonly Stack<PixelArtEditorSnapshot> _undoStack = new();
    private readonly Stack<PixelArtEditorSnapshot> _redoStack = new();

    // Observable Properties
    [ObservableProperty] private Color _backgroundColor = Colors.White;
    [ObservableProperty] private Color _selectedColor = Colors.Black;
    [ObservableProperty] private int _columns = 32;
    [ObservableProperty] private int _rows = 32;
    [ObservableProperty] private double _zoomLevel = 1.0;
    [ObservableProperty] private string _iconName = "Pixel_Art";
    [ObservableProperty] private string _exportFolder = string.Empty;
    [ObservableProperty] private PixelTool _selectedTool = PixelTool.Pencil;
    [ObservableProperty] private bool _isGridVisible = true;
    [ObservableProperty] private bool _useTransparentBackground = false;

    [ObservableProperty] private string _documentName = "Untitled";
    [ObservableProperty] private bool _isRenamingDocumentName;

    [ObservableProperty] private int _pencilSize = 1;
    [ObservableProperty] private int _brushSize = 3;
    [ObservableProperty] private double _brushHardness = 1.0;
    [ObservableProperty] private int _eraserSize = 1;
    [ObservableProperty] private bool _fillAllowDiagonal = false;
    private bool _isSelectedColorPickerOpen;
    private bool _isBackgroundColorPickerOpen;

    public PixelArtEditorViewModel(INotificationService notificationService, ISettingsService settingsService,
        ILoggingService loggingService, IFileDialogService fileDialogService, PixelArtExportService exportService)
    {
        _notificationService = notificationService;
        _settingsService = settingsService;
        _loggingService = loggingService;
        _fileDialogService = fileDialogService;
        _exportService = exportService;

        _loggingService.LogInfo("PixelArtEditorViewModel initialized.");
        _exportFolder = _settingsService.Settings.Application.ExportLocation;

        ResizeBuffer(Rows, Columns);

        Layers.Add(new PixelArtLayerViewModel("Layer 1", Rows, Columns));
        SelectedLayer = Layers[0];
        _layerCounter = 1;
    }

    partial void OnRowsChanged(int value)
    {
        _loggingService.LogInfo($"Rows changed to: {value}");
    }

    partial void OnColumnsChanged(int value)
    {
        _loggingService.LogInfo($"Columns changed to: {value}");
    }

    partial void OnBackgroundColorChanged(Color value)
    {
        _loggingService.LogInfo($"Background color changed to: {value}");
        BackgroundInvalidated?.Invoke();
    }

    partial void OnSelectedColorChanged(Color value)
    {
        _loggingService.LogInfo($"Selected color changed to: {value}");
    }

    partial void OnZoomLevelChanged(double value)
    {
        _loggingService.LogInfo($"Zoom level changed to: {value}");
    }

    partial void OnUseTransparentBackgroundChanged(bool value)
    {
        _loggingService.LogInfo($"Use transparent background changed to: {value}");
        BackgroundInvalidated?.Invoke();
    }

    partial void OnExportFolderChanged(string value)
    {
        _settingsService.Settings.Application.ExportLocation = value;
        OnPropertyChanged(nameof(ExportFolderDisplay));
        OnPropertyChanged(nameof(ExportFolderStatus));
        OnPropertyChanged(nameof(ExportFolderActionText));
        OnPropertyChanged(nameof(HasExportFolder));
        OnPropertyChanged(nameof(ExportFolderExists));
        ExportCommand.NotifyCanExecuteChanged();
    }

    partial void OnDocumentNameChanged(string value)
    {
        OnPropertyChanged(nameof(ExportFileSummary));
    }

    [RelayCommand]
    private void OpenSelectedColorPicker()
    {
        IsBackgroundColorPickerOpen = false;
        IsSelectedColorPickerOpen = true;
    }

    [RelayCommand]
    private void OpenBackgroundColorPicker()
    {
        IsSelectedColorPickerOpen = false;
        IsBackgroundColorPickerOpen = true;
    }

    [RelayCommand]
    private void CloseColorPickers()
    {
        IsSelectedColorPickerOpen = false;
        IsBackgroundColorPickerOpen = false;
    }

    public event Action? LayoutInvalidated;
    public event Action? BackgroundInvalidated;
    public event Action<int, Color>? PixelChanged;

    public ObservableCollection<Color> RecentColors { get; } = new();

    public ObservableCollection<PixelArtLayerViewModel> Layers { get; } = new();

    private PixelArtLayerViewModel? _selectedLayer;

    public PixelArtLayerViewModel? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            if (!SetProperty(ref _selectedLayer, value)) return;
            if (value is null) return;
            CompositeLayersIntoBuffer();
            LayoutInvalidated?.Invoke();
        }
    }

    public void CommitRecentColor(Color color)
    {
        AddRecentColor(color);
    }

    public bool IsSelectedColorPickerOpen
    {
        get => _isSelectedColorPickerOpen;
        set => SetProperty(ref _isSelectedColorPickerOpen, value);
    }

    private bool CanAddLayer() => Rows > 0 && Columns > 0;

    [RelayCommand(CanExecute = nameof(CanAddLayer))]
    private void AddLayer()
    {
        PushUndoSnapshot();
        var name = $"Layer {++_layerCounter}";
        var layer = new PixelArtLayerViewModel(name, Rows, Columns);
        Layers.Add(layer);
        SelectedLayer = layer;
        CompositeLayersIntoBuffer();
        LayoutInvalidated?.Invoke();
    }

    private bool CanRemoveLayer(PixelArtLayerViewModel? layer)
        => layer is not null && Layers.Count > 1;

    [RelayCommand(CanExecute = nameof(CanRemoveLayer))]
    private void RemoveLayer(PixelArtLayerViewModel? layer)
    {
        if (layer is null) return;
        if (Layers.Count <= 1) return;

        PushUndoSnapshot();
        var index = Layers.IndexOf(layer);
        Layers.Remove(layer);

        if (SelectedLayer == layer)
        {
            SelectedLayer = Layers[Math.Clamp(index - 1, 0, Layers.Count - 1)];
        }

        CompositeLayersIntoBuffer();
        LayoutInvalidated?.Invoke();
    }

    [RelayCommand]
    private void ToggleLayerLock(PixelArtLayerViewModel? layer)
    {
        if (layer is null) return;
        PushUndoSnapshot();
        layer.IsLocked = !layer.IsLocked;
    }

    [RelayCommand]
    private void BeginRenameLayer(PixelArtLayerViewModel? layer)
    {
        if (layer is null) return;
        SelectedLayer = layer;
        layer.IsRenaming = true;
    }

    [RelayCommand]
    private void BeginRenameDocumentName()
    {
        IsRenamingDocumentName = true;
    }

    private void AddRecentColor(Color color)
    {
        // Avoid adding fully-transparent colors; the editor is ARGB but UI is primarily opaque.
        if (color.A == 0) return;

        for (var i = 0; i < RecentColors.Count; i++)
        {
            if (RecentColors[i] == color)
            {
                if (i == 0) return;
                RecentColors.RemoveAt(i);
                break;
            }
        }

        RecentColors.Insert(0, color);

        while (RecentColors.Count > MaxRecentColors)
        {
            RecentColors.RemoveAt(RecentColors.Count - 1);
        }
    }

    public bool IsBackgroundColorPickerOpen
    {
        get => _isBackgroundColorPickerOpen;
        set => SetProperty(ref _isBackgroundColorPickerOpen, value);
    }

    public bool HasExportFolder => !string.IsNullOrWhiteSpace(ExportFolder);

    public bool ExportFolderExists => HasExportFolder && Directory.Exists(ExportFolder);

    public string ExportFolderDisplay => HasExportFolder
        ? ExportFolder
        : "No folder selected";

    public string ExportFolderActionText => HasExportFolder ? "Change Folder" : "Choose Folder";

    public string ExportFileSummary => $"{GetExportBaseName()}.png + {GetExportBaseName()}.ico";

    public string ExportFolderStatus
    {
        get
        {
            if (!HasExportFolder) return "No export folder selected";
            return ExportFolderExists
                ? "Ready to export PNG and ICO files"
                : "Folder will be created on export";
        }
    }

    public ReadOnlySpan<uint> PixelsArgb => _pixelsArgb;

    [RelayCommand]
    private void ApplyLayout()
    {
        _loggingService.LogInfo($"Applying layout with Rows: {Rows}, Columns: {Columns}");

        if (!PerformMaxSizeValidation(Rows, Columns))
        {
            _loggingService.LogWarning($"Layout validation failed for grid size: {Rows}x{Columns}");
            return;
        }

        var expectedLength = checked(Rows * Columns);
        var layoutChanged = _pixelsArgb.Length != expectedLength || Layers.Any(layer => layer.PixelsArgb.Length != expectedLength);
        if (layoutChanged)
        {
            PushUndoSnapshot();
        }

        ResizeBuffer(Rows, Columns);
        
        foreach (var layer in Layers)
        {
            layer.Resize(Rows, Columns);
        }
        
        ClearToBackground();
        CompositeLayersIntoBuffer();
        LayoutInvalidated?.Invoke();
    }

    private bool PerformMaxSizeValidation(int rows, int columns)
    {
        const int maxRows = 512;
        const int maxColumns = 512;

        if (rows <= 0 || columns <= 0)
        {
            _loggingService.LogWarning($"Grid size must be positive - Rows: {rows}, Columns: {columns}");
            _notificationService.AddNotification("Pixel Art Editor", "Grid size must be greater than zero.", NotificationType.Warning);
            return false;
        }

        if (rows <= maxRows && columns <= maxColumns)
        {
            return true;
        }

        _loggingService.LogWarning(
            $"Grid size validation failed - Rows: {Rows} (max: {maxRows}), Columns: {Columns} (max: {maxColumns})");

        var message = $"The grid size exceeds the maximum allowed values.\n\n" +
                      $"Maximum Rows: {maxRows}\nMaximum Columns: {maxColumns}";

        _notificationService.AddNotification("Pixel Art Editor", message, NotificationType.Warning);

        return false;
    }

    [RelayCommand]
    private void New()
    {
        PushUndoSnapshot();
        ClearLayers();
        CompositeLayersIntoBuffer();
        LayoutInvalidated?.Invoke();
    }

    [RelayCommand]
    private void ImportImage()
    {
        var selected = _fileDialogService.OpenFiles(
            "Import image into pixel editor",
            "Images and icons (*.png;*.jpg;*.jpeg;*.ico)|*.png;*.jpg;*.jpeg;*.ico",
            false);

        var path = selected.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(path))
            return;

        ImportImageFromPath(path);
    }

    public void ImportImageFromPath(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                _notificationService.AddNotification("Pixel Art Editor", "The selected image could not be found.", NotificationType.Warning);
                return;
            }

            var decoder = BitmapDecoder.Create(new Uri(path), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames.OrderByDescending(f => f.PixelWidth * f.PixelHeight).First();
            var importScale = Math.Min(1.0, MaxImportDimension / (double)Math.Max(frame.PixelWidth, frame.PixelHeight));
            var width = Math.Max(1, (int)Math.Round(frame.PixelWidth * importScale));
            var height = Math.Max(1, (int)Math.Round(frame.PixelHeight * importScale));
            BitmapSource source = frame;

            if (frame.PixelWidth != width || frame.PixelHeight != height)
            {
                source = new TransformedBitmap(frame, new ScaleTransform(
                    (double)width / Math.Max(1, frame.PixelWidth),
                    (double)height / Math.Max(1, frame.PixelHeight)));
            }

            var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            var stride = width * 4;
            var pixels = new byte[stride * height];
            converted.CopyPixels(pixels, stride, 0);

            PushUndoSnapshot();
            Rows = height;
            Columns = width;
            DocumentName = Path.GetFileNameWithoutExtension(path);
            ResizeBuffer(Rows, Columns);
            Layers.Clear();

            var layer = new PixelArtLayerViewModel("Imported Image", Rows, Columns);
            for (var i = 0; i < Rows * Columns; i++)
            {
                var offset = i * 4;
                var b = pixels[offset + 0];
                var g = pixels[offset + 1];
                var r = pixels[offset + 2];
                var a = pixels[offset + 3];
                layer.PixelsArgb[i] = (uint)(a << 24 | r << 16 | g << 8 | b);
            }

            Layers.Add(layer);
            SelectedLayer = layer;
            _layerCounter = 1;
            CompositeLayersIntoBuffer();
            LayoutInvalidated?.Invoke();
            var resizedMessage = importScale < 1.0 ? $" and resized to {width}x{height}" : string.Empty;
            _notificationService.AddNotification("Pixel Art Editor", $"Imported {Path.GetFileName(path)}{resizedMessage}.", NotificationType.Success);
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Image import failed", ex);
            _notificationService.AddNotification("Pixel Art Editor", "Failed to import the selected image.", NotificationType.Error);
        }
    }

    private bool CanUndo() => _undoStack.Count > 0;

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (_undoStack.Count == 0) return;

        _redoStack.Push(CreateSnapshot());
        RestoreSnapshot(_undoStack.Pop());
        NotifyHistoryCommandsChanged();
    }

    private bool CanRedo() => _redoStack.Count > 0;

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_redoStack.Count == 0) return;

        _undoStack.Push(CreateSnapshot());
        RestoreSnapshot(_redoStack.Pop());
        NotifyHistoryCommandsChanged();
    }

    public void BeginStroke(int cellIndex)
    {
        _activeStrokeIndex = -1;
        _strokeHasHistorySnapshot = false;
        ApplyToolAt(cellIndex);
    }

    public void ContinueStroke(int cellIndex)
    {
        if (cellIndex == _activeStrokeIndex) return;
        ApplyToolAt(cellIndex);
    }

    public void EndStroke()
    {
        _activeStrokeIndex = -1;
        _strokeHasHistorySnapshot = false;
    }

    public void ApplyToolAt(int cellIndex)
    {
        if ((uint)cellIndex >= (uint)_pixelsArgb.Length) return;
        if (!CanSelectedLayerChangeAt(cellIndex)) return;

        if (!_strokeHasHistorySnapshot)
        {
            PushUndoSnapshot();
            _strokeHasHistorySnapshot = true;
        }

        _activeStrokeIndex = cellIndex;

        switch (SelectedTool)
        {
            case PixelTool.Pencil:
                StampAt(cellIndex, SelectedColor, Math.Max(1, PencilSize));
                break;
            case PixelTool.Brush:
                StampSoftAt(cellIndex, SelectedColor, Math.Max(1, BrushSize), Math.Clamp(BrushHardness, 0.05, 1.0));
                break;
            case PixelTool.Eraser:
                StampTransparentAt(cellIndex, Math.Max(1, EraserSize));
                break;
            case PixelTool.Fill:
                FloodFill(cellIndex, SelectedColor);
                break;
            default:
                StampAt(cellIndex, SelectedColor, 1);
                break;
        }
    }

    private bool CanSelectedLayerChangeAt(int cellIndex)
    {
        if (SelectedLayer is null) return false;
        if (SelectedLayer.IsLocked) return false;
        if ((uint)cellIndex >= (uint)SelectedLayer.PixelsArgb.Length) return false;
        return !IsProtectedByLockedLayer(cellIndex);
    }

    private void StampAt(int centerIndex, Color color, int size)
    {
        if (SelectedLayer is null) return;
        if (SelectedLayer.IsLocked) return;

        var radius = Math.Max(0, (size - 1) / 2);
        if (radius == 0)
        {
            SetCellColor(centerIndex, color);
            return;
        }

        var centerRow = centerIndex / Columns;
        var centerCol = centerIndex % Columns;

        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                if ((dx * dx + dy * dy) > (radius * radius)) continue;

                var r = centerRow + dy;
                var c = centerCol + dx;
                if ((uint)r >= (uint)Rows || (uint)c >= (uint)Columns) continue;

                var idx = r * Columns + c;
                SetCellColor(idx, color);
            }
        }
    }

    private void StampSoftAt(int centerIndex, Color color, int size, double hardness)
    {
        if (SelectedLayer is null) return;
        if (SelectedLayer.IsLocked) return;

        var radius = Math.Max(0, (size - 1) / 2);
        if (radius == 0)
        {
            SetCellColor(centerIndex, color);
            return;
        }

        var centerRow = centerIndex / Columns;
        var centerCol = centerIndex % Columns;
        var argb = ToArgb(color);

        // Simple "soft" mask: nearer pixels always apply; edge pixels apply based on hardness threshold.
        // This keeps behavior deterministic without introducing alpha blending.
        var hardRadius = Math.Max(0, (int)Math.Round(radius * hardness));
        var hardRadiusSq = hardRadius * hardRadius;
        var radiusSq = radius * radius;

        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                var distSq = (dx * dx + dy * dy);
                if (distSq > radiusSq) continue;

                // Soft edge selection: skip some edge pixels when hardness < 1.
                if (distSq > hardRadiusSq)
                {
                    var t = (distSq - hardRadiusSq) / (double)Math.Max(1, radiusSq - hardRadiusSq);
                    if (t > 0.66) continue;
                }

                var r = centerRow + dy;
                var c = centerCol + dx;
                if ((uint)r >= (uint)Rows || (uint)c >= (uint)Columns) continue;

                var idx = r * Columns + c;
                if (IsProtectedByLockedLayer(idx)) continue;

                var layerPixels = SelectedLayer.PixelsArgb;
                if ((uint)idx >= (uint)layerPixels.Length) continue;
                if (layerPixels[idx] == argb) continue;

                layerPixels[idx] = argb;
                CompositeLayersIntoBuffer();
                PixelChanged?.Invoke(idx, FromArgb(_pixelsArgb[idx]));
            }
        }
    }

    private void StampTransparentAt(int centerIndex, int size)
    {
        if (SelectedLayer is null) return;
        if (SelectedLayer.IsLocked) return;

        var radius = Math.Max(0, (size - 1) / 2);
        if (radius == 0)
        {
            SetCellTransparent(centerIndex);
            return;
        }

        var centerRow = centerIndex / Columns;
        var centerCol = centerIndex % Columns;

        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                if ((dx * dx + dy * dy) > (radius * radius)) continue;

                var r = centerRow + dy;
                var c = centerCol + dx;
                if ((uint)r >= (uint)Rows || (uint)c >= (uint)Columns) continue;

                SetCellTransparent(r * Columns + c);
            }
        }
    }

    private bool CanExport()
        => HasExportFolder;

    [RelayCommand]
    private async Task BrowseExportFolderAsync()
    {
        var selected = _fileDialogService.BrowseForFolder(ExportFolder, "Select pixel art export folder");
        if (string.IsNullOrWhiteSpace(selected)) return;

        ExportFolder = selected;
        await _settingsService.SaveSettingsAsync();
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        try
        {
            var folder = ExportFolder;
            if (string.IsNullOrWhiteSpace(folder))
            {
                _notificationService.AddNotification("Pixel Art Editor", "Choose an export folder before exporting.", NotificationType.Warning);
                return;
            }

            var name = GetExportBaseName();
            _exportService.ExportPngAndIco(folder, name, Rows, Columns, PixelsArgb, BackgroundColor, UseTransparentBackground);
            await _settingsService.SaveSettingsAsync();
            OnPropertyChanged(nameof(ExportFolderStatus));
            OnPropertyChanged(nameof(ExportFolderExists));
            _notificationService.AddNotification("Pixel Art Editor", $"Exported {name}.png and {name}.ico", NotificationType.Success);
        }
        catch (InvalidOperationException ex)
        {
            _loggingService.LogError("Export failed", ex);
            _notificationService.AddNotification("Pixel Art Editor", ex.Message, NotificationType.Error);
        }
    }

    private void ResizeBuffer(int rows, int columns)
    {
        if (rows <= 0 || columns <= 0)
        {
            _pixelsArgb = Array.Empty<uint>();
            return;
        }

        var length = checked(rows * columns);
        _pixelsArgb = new uint[length];
    }

    private void ClearToBackground()
    {
        var bg = ToArgb(BackgroundColor);
        Array.Fill(_pixelsArgb, bg);
    }

    private void ClearLayers()
    {
        foreach (var layer in Layers)
        {
            Array.Clear(layer.PixelsArgb);
        }
    }

    private void CompositeLayersIntoBuffer()
    {
        if (_pixelsArgb.Length == 0)
        {
            return;
        }

        // Fill with background color or transparent based on user setting
        var fillColor = UseTransparentBackground ? 0u : ToArgb(BackgroundColor);
        Array.Fill(_pixelsArgb, fillColor);

        // 1) Composite unlocked layers in stacking order.
        for (var i = 0; i < Layers.Count; i++)
        {
            var layer = Layers[i];
            if (layer.IsLocked) continue;

            var src = layer.PixelsArgb;
            if (src.Length != _pixelsArgb.Length) continue;

            for (var p = 0; p < src.Length; p++)
            {
                var argb = src[p];
                if ((argb >> 24) == 0) continue;
                _pixelsArgb[p] = argb;
            }
        }

        // 2) Apply locked pixels last so they are "protected" and always visible.
        for (var i = 0; i < Layers.Count; i++)
        {
            var layer = Layers[i];
            if (!layer.IsLocked) continue;

            var src = layer.PixelsArgb;
            if (src.Length != _pixelsArgb.Length) continue;

            for (var p = 0; p < src.Length; p++)
            {
                var argb = src[p];
                if ((argb >> 24) == 0) continue;
                _pixelsArgb[p] = argb;
            }
        }
    }

    private bool IsProtectedByLockedLayer(int index)
    {
        for (var i = 0; i < Layers.Count; i++)
        {
            var layer = Layers[i];
            if (!layer.IsLocked) continue;
            var pixels = layer.PixelsArgb;
            if ((uint)index >= (uint)pixels.Length) continue;
            if ((pixels[index] >> 24) != 0) return true;
        }

        return false;
    }

    private void SetCellColor(int index, Color color)
    {
        if (SelectedLayer is null) return;
        if (SelectedLayer.IsLocked) return;
        if (IsProtectedByLockedLayer(index)) return;

        var argb = ToArgb(color);
        var layerPixels = SelectedLayer.PixelsArgb;
        if ((uint)index >= (uint)layerPixels.Length) return;
        if (layerPixels[index] == argb) return;

        layerPixels[index] = argb;
        CompositeLayersIntoBuffer();
        PixelChanged?.Invoke(index, FromArgb(_pixelsArgb[index]));
    }

    private void SetCellTransparent(int index)
    {
        if (SelectedLayer is null) return;
        if (SelectedLayer.IsLocked) return;
        if (IsProtectedByLockedLayer(index)) return;

        var layerPixels = SelectedLayer.PixelsArgb;
        if ((uint)index >= (uint)layerPixels.Length) return;
        if (layerPixels[index] == 0) return;

        layerPixels[index] = 0;
        CompositeLayersIntoBuffer();
        PixelChanged?.Invoke(index, FromArgb(_pixelsArgb[index]));
    }

    private void FloodFill(int startIndex, Color color)
    {
        if (_pixelsArgb.Length == 0) return;
        if (SelectedLayer is null) return;
        if (SelectedLayer.IsLocked) return;
        if (IsProtectedByLockedLayer(startIndex)) return;

        var layerPixels = SelectedLayer.PixelsArgb;
        if ((uint)startIndex >= (uint)layerPixels.Length) return;

        var target = layerPixels[startIndex];
        var replacement = ToArgb(color);
        if (target == replacement) return;

        var visited = new bool[_pixelsArgb.Length];
        var queue = new Queue<int>();
        queue.Enqueue(startIndex);
        visited[startIndex] = true;

        while (queue.Count > 0)
        {
            var index = queue.Dequeue();
            if (layerPixels[index] != target) continue;
            if (IsProtectedByLockedLayer(index)) continue;

            layerPixels[index] = replacement;
            CompositeLayersIntoBuffer();
            PixelChanged?.Invoke(index, FromArgb(_pixelsArgb[index]));

            var row = index / Columns;
            var col = index % Columns;

            EnqueueIfUnvisited(index - 1, col > 0);
            EnqueueIfUnvisited(index + 1, col < Columns - 1);
            EnqueueIfUnvisited(index - Columns, row > 0);
            EnqueueIfUnvisited(index + Columns, row < Rows - 1);

            if (FillAllowDiagonal)
            {
                EnqueueIfUnvisited(index - Columns - 1, row > 0 && col > 0);
                EnqueueIfUnvisited(index - Columns + 1, row > 0 && col < Columns - 1);
                EnqueueIfUnvisited(index + Columns - 1, row < Rows - 1 && col > 0);
                EnqueueIfUnvisited(index + Columns + 1, row < Rows - 1 && col < Columns - 1);
            }

            void EnqueueIfUnvisited(int neighborIndex, bool condition)
            {
                if (!condition) return;
                if (visited[neighborIndex]) return;
                visited[neighborIndex] = true;
                queue.Enqueue(neighborIndex);
            }
        }
    }

    private static uint ToArgb(Color c) => (uint)(c.A << 24 | c.R << 16 | c.G << 8 | c.B);

    private string GetExportBaseName()
        => string.IsNullOrWhiteSpace(DocumentName)
            ? (string.IsNullOrWhiteSpace(IconName) ? "Pixel_Art" : IconName)
            : DocumentName;

    private void PushUndoSnapshot()
    {
        _undoStack.Push(CreateSnapshot());
        TrimHistory(_undoStack);
        _redoStack.Clear();
        NotifyHistoryCommandsChanged();
    }

    private PixelArtEditorSnapshot CreateSnapshot()
    {
        var selectedLayerIndex = SelectedLayer is null ? -1 : Layers.IndexOf(SelectedLayer);
        var layerSnapshots = Layers
            .Select(layer => new PixelArtLayerSnapshot(layer.Name, layer.IsLocked, layer.PixelsArgb.ToArray()))
            .ToArray();

        return new PixelArtEditorSnapshot(Rows, Columns, BackgroundColor, selectedLayerIndex, _layerCounter, layerSnapshots);
    }

    private void RestoreSnapshot(PixelArtEditorSnapshot snapshot)
    {
        Rows = snapshot.Rows;
        Columns = snapshot.Columns;
        BackgroundColor = snapshot.BackgroundColor;
        _layerCounter = snapshot.LayerCounter;

        ResizeBuffer(Rows, Columns);
        Layers.Clear();

        foreach (var layerSnapshot in snapshot.Layers)
        {
            var layer = new PixelArtLayerViewModel(layerSnapshot.Name, Rows, Columns)
            {
                IsLocked = layerSnapshot.IsLocked
            };

            Array.Copy(layerSnapshot.PixelsArgb, layer.PixelsArgb, Math.Min(layerSnapshot.PixelsArgb.Length, layer.PixelsArgb.Length));
            Layers.Add(layer);
        }

        if (Layers.Count == 0)
        {
            Layers.Add(new PixelArtLayerViewModel("Layer 1", Rows, Columns));
            _layerCounter = 1;
        }

        var selectedIndex = Math.Clamp(snapshot.SelectedLayerIndex, 0, Layers.Count - 1);
        SelectedLayer = Layers[selectedIndex];
        CompositeLayersIntoBuffer();
        LayoutInvalidated?.Invoke();
    }

    private void NotifyHistoryCommandsChanged()
    {
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private static void TrimHistory(Stack<PixelArtEditorSnapshot> stack)
    {
        if (stack.Count <= MaxHistoryEntries) return;

        var entries = stack.Reverse().Skip(stack.Count - MaxHistoryEntries).ToArray();
        stack.Clear();

        foreach (var entry in entries)
        {
            stack.Push(entry);
        }
    }

    private static Color FromArgb(uint argb)
    {
        var a = (byte)((argb >> 24) & 0xFF);
        var r = (byte)((argb >> 16) & 0xFF);
        var g = (byte)((argb >> 8) & 0xFF);
        var b = (byte)(argb & 0xFF);
        return Color.FromArgb(a, r, g, b);
    }

    private sealed record PixelArtEditorSnapshot(
        int Rows,
        int Columns,
        Color BackgroundColor,
        int SelectedLayerIndex,
        int LayerCounter,
        PixelArtLayerSnapshot[] Layers);

    private sealed record PixelArtLayerSnapshot(string Name, bool IsLocked, uint[] PixelsArgb);
}
