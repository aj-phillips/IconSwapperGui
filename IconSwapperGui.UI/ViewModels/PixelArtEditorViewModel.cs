using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.PixelArt;
using System.Collections.ObjectModel;
using System.Windows.Media;
using IconSwapperGui.UI.Services.PixelArtEditor;
using System;

namespace IconSwapperGui.UI.ViewModels;

public partial class PixelArtEditorViewModel : ObservableObject
{
    private const int MaxRecentColors = 12;

    private readonly INotificationService _notificationService;
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;
    private readonly PixelArtExportService _exportService;

    private uint[] _pixelsArgb = Array.Empty<uint>();
    private int _activeStrokeIndex = -1;
    private int _layerCounter = 1;

    // Observable Properties
    [ObservableProperty] private Color _backgroundColor = Colors.White;
    [ObservableProperty] private Color _selectedColor = Colors.Black;
    [ObservableProperty] private int _columns = 32;
    [ObservableProperty] private int _rows = 32;
    [ObservableProperty] private double _zoomLevel = 1.0;
    [ObservableProperty] private string _iconName = "Pixel_Art";
    [ObservableProperty] private PixelTool _selectedTool = PixelTool.Pencil;
    [ObservableProperty] private bool _isGridVisible = true;

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
        ILoggingService loggingService, PixelArtExportService exportService)
    {
        _notificationService = notificationService;
        _settingsService = settingsService;
        _loggingService = loggingService;
        _exportService = exportService;

        _loggingService.LogInfo("PixelArtEditorViewModel initialized.");

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
    }

    partial void OnSelectedColorChanged(Color value)
    {
        _loggingService.LogInfo($"Selected color changed to: {value}");
    }

    partial void OnZoomLevelChanged(double value)
    {
        _loggingService.LogInfo($"Zoom level changed to: {value}");
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

    public string ExportFolderDisplay => string.IsNullOrWhiteSpace(_settingsService.Settings.Application.ExportLocation)
        ? "Not set (Settings → Application → Export Location)"
        : _settingsService.Settings.Application.ExportLocation;

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
        ClearToBackground();
        LayoutInvalidated?.Invoke();
    }

    public void BeginStroke(int cellIndex)
    {
        _activeStrokeIndex = -1;
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
    }

    public void ApplyToolAt(int cellIndex)
    {
        if ((uint)cellIndex >= (uint)_pixelsArgb.Length) return;

        _activeStrokeIndex = cellIndex;

        switch (_selectedTool)
        {
            case PixelTool.Pencil:
                StampAt(cellIndex, SelectedColor, Math.Max(1, PencilSize));
                break;
            case PixelTool.Brush:
                StampSoftAt(cellIndex, SelectedColor, Math.Max(1, BrushSize), Math.Clamp(BrushHardness, 0.05, 1.0));
                break;
            case PixelTool.Eraser:
                StampAt(cellIndex, BackgroundColor, Math.Max(1, EraserSize));
                break;
            case PixelTool.Fill:
                FloodFill(cellIndex, SelectedColor);
                break;
            default:
                StampAt(cellIndex, SelectedColor, 1);
                break;
        }
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

    private bool CanExport()
        => !string.IsNullOrWhiteSpace(_settingsService.Settings.Application.ExportLocation);

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void Export()
    {
        try
        {
            var folder = _settingsService.Settings.Application.ExportLocation;
            if (string.IsNullOrWhiteSpace(folder))
            {
                _notificationService.AddNotification("Pixel Art Editor", "No export folder configured in Settings.", NotificationType.Warning);
                return;
            }

            var name = string.IsNullOrWhiteSpace(DocumentName)
                ? (string.IsNullOrWhiteSpace(IconName) ? "Pixel_Art" : IconName)
                : DocumentName;
            _exportService.ExportPngAndIco(folder, name, Rows, Columns, PixelsArgb, BackgroundColor);
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

    private void CompositeLayersIntoBuffer()
    {
        if (_pixelsArgb.Length == 0)
        {
            return;
        }

        var bg = ToArgb(BackgroundColor);
        Array.Fill(_pixelsArgb, bg);

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

    private static Color FromArgb(uint argb)
    {
        var a = (byte)((argb >> 24) & 0xFF);
        var r = (byte)((argb >> 16) & 0xFF);
        var g = (byte)((argb >> 8) & 0xFF);
        var b = (byte)(argb & 0xFF);
        return Color.FromArgb(a, r, g, b);
    }
}