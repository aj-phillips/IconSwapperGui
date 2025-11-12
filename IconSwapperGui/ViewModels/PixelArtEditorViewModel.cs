using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.PixelArtEditor;
using Serilog;

namespace IconSwapperGui.ViewModels;

public partial class PixelArtEditorViewModel : ObservableObject
{
    private readonly ILogger _logger = Log.ForContext<PixelArtEditorViewModel>();

    [ObservableProperty] private Color _backgroundColor = Colors.White;

    [ObservableProperty] private int _columns = 32;

    [ObservableProperty] private Canvas? _drawableCanvas;

    [ObservableProperty] private int _rows = 32;

    [ObservableProperty] private Color _selectedColor = Colors.Black;

    [ObservableProperty] private double _zoomLevel = 1.0;

    public PixelArtEditorViewModel()
    {
        _logger.Information("PixelArtEditorViewModel initializing");

        Pixels = new ObservableCollection<Rectangle>();
        ApplyLayoutCommand = new RelayCommand(_ => ApplyLayout());
        DrawableCanvasMouseLeftButtonDownCommand =
            new RelayCommand(e => DrawableCanvas_MouseLeftButtonDown((MouseButtonEventArgs)e));
        DrawableCanvasMouseMoveCommand = new RelayCommand(e => DrawableCanvas_MouseMove((MouseEventArgs)e));
        DrawableCanvasMouseRightButtonDownCommand =
            new RelayCommand(e => DrawableCanvas_MouseRightButtonDown((MouseButtonEventArgs)e));
        ZoomSliderValueChangedCommand =
            new RelayCommand(e => ZoomSlider_ValueChanged((RoutedPropertyChangedEventArgs<double>)e));
        ExportIconCommand = new ExportIconCommand(this, null!, _ => true);

        _logger.Information(
            "PixelArtEditorViewModel initialized with default grid size: {Rows}x{Columns}, BackgroundColor: {BackgroundColor}, SelectedColor: {SelectedColor}",
            Rows, Columns, BackgroundColor, SelectedColor);
    }

    public ObservableCollection<Rectangle> Pixels { get; }
    public ICommand ApplyLayoutCommand { get; private set; }
    public ICommand DrawableCanvasMouseLeftButtonDownCommand { get; private set; }
    public ICommand DrawableCanvasMouseMoveCommand { get; private set; }
    public ICommand DrawableCanvasMouseRightButtonDownCommand { get; private set; }
    public ICommand ZoomSliderValueChangedCommand { get; private set; }
    public RelayCommand ExportIconCommand { get; }

    partial void OnDrawableCanvasChanged(Canvas? value)
    {
        _logger.Information("DrawableCanvas changed, applying layout");
        ApplyLayout();
    }

    partial void OnRowsChanged(int value)
    {
        _logger.Information("Rows changed to: {Rows}", value);
    }

    partial void OnColumnsChanged(int value)
    {
        _logger.Information("Columns changed to: {Columns}", value);
    }

    partial void OnBackgroundColorChanged(Color value)
    {
        _logger.Information("Background color changed to: {BackgroundColor}", value);
    }

    partial void OnSelectedColorChanged(Color value)
    {
        _logger.Information("Selected color changed to: {SelectedColor}", value);
    }

    partial void OnZoomLevelChanged(double value)
    {
        _logger.Information("Zoom level changed to: {ZoomLevel}", value);
    }

    private void ApplyLayout()
    {
        _logger.Information("Applying layout with Rows: {Rows}, Columns: {Columns}", Rows, Columns);

        if (DrawableCanvas == null)
        {
            _logger.Warning("Cannot apply layout - DrawableCanvas is null");
            return;
        }

        if (!PerformMaxSizeValidation(Rows, Columns))
        {
            _logger.Warning("Layout validation failed for grid size: {Rows}x{Columns}", Rows, Columns);
            return;
        }

        try
        {
            DrawableCanvas.Children.Clear();
            Pixels.Clear();

            var widthFactor = DrawableCanvas.ActualWidth / Columns;
            var heightFactor = DrawableCanvas.ActualHeight / Rows;
            var cellSize = Math.Min(widthFactor, heightFactor);

            var totalWidth = Columns * cellSize;
            var totalHeight = Rows * cellSize;
            var xScale = DrawableCanvas.ActualWidth / totalWidth;
            var yScale = DrawableCanvas.ActualHeight / totalHeight;
            var scale = Math.Min(xScale, yScale);

            cellSize *= scale;

            _logger.Information("Calculated cell size: {CellSize}, Canvas size: {CanvasWidth}x{CanvasHeight}",
                cellSize, DrawableCanvas.ActualWidth, DrawableCanvas.ActualHeight);

            for (var row = 0; row < Rows; row++)
            {
                for (var col = 0; col < Columns; col++)
                {
                    var rect = new Rectangle
                    {
                        Width = cellSize,
                        Height = cellSize,
                        Stroke = Brushes.Black,
                        Fill = new SolidColorBrush(BackgroundColor),
                        StrokeThickness = 0.5
                    };
                    Canvas.SetLeft(rect, col * cellSize);
                    Canvas.SetTop(rect, row * cellSize);
                    DrawableCanvas.Children.Add(rect);
                    Pixels.Add(rect);
                }
            }

            _logger.Information("Successfully applied layout, created {PixelCount} pixels ({Rows}x{Columns})",
                Pixels.Count, Rows, Columns);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error applying layout with Rows: {Rows}, Columns: {Columns}", Rows, Columns);
        }
    }

    private bool PerformMaxSizeValidation(int rows, int columns)
    {
        const int maxRows = 96;
        const int maxColumns = 96;

        if (rows <= maxRows && columns <= maxColumns)
        {
            return true;
        }

        _logger.Warning(
            "Grid size validation failed - Rows: {Rows} (max: {MaxRows}), Columns: {Columns} (max: {MaxColumns})",
            rows, maxRows, columns, maxColumns);

        var message = $"The grid size exceeds the maximum allowed values.\n\n" +
                      $"Maximum Rows: {maxRows}\nMaximum Columns: {maxColumns}";

        MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);

        return false;
    }

    private void DrawableCanvas_MouseLeftButtonDown(MouseButtonEventArgs? e)
    {
        if (e == null)
        {
            _logger.Warning("MouseLeftButtonDown event args is null");
            return;
        }

        var position = e.GetPosition(DrawableCanvas);
        _logger.Information("Mouse left button down at position: ({X}, {Y})", position.X, position.Y);
        DrawPixel(position);
    }

    private void DrawableCanvas_MouseMove(MouseEventArgs? e)
    {
        if (e == null) return;

        var position = e.GetPosition(DrawableCanvas);

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DrawPixel(position);
        }
        else if (e.RightButton == MouseButtonState.Pressed)
        {
            ResetPixel(position);
        }
    }

    private void DrawableCanvas_MouseRightButtonDown(MouseButtonEventArgs? e)
    {
        if (e == null)
        {
            _logger.Warning("MouseRightButtonDown event args is null");
            return;
        }

        var position = e.GetPosition(DrawableCanvas);
        _logger.Information("Mouse right button down at position: ({X}, {Y})", position.X, position.Y);
        ResetPixel(position);
    }

    private void ZoomSlider_ValueChanged(RoutedPropertyChangedEventArgs<double> e)
    {
        ZoomLevel = e.NewValue;
    }

    private void DrawPixel(Point position)
    {
        try
        {
            foreach (var rect in Pixels)
            {
                if (IsPointInRectangle(position, rect))
                {
                    rect.Fill = new SolidColorBrush(SelectedColor);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error drawing pixel at position: ({X}, {Y})", position.X, position.Y);
        }
    }

    private void ResetPixel(Point position)
    {
        try
        {
            foreach (var rect in Pixels)
            {
                if (IsPointInRectangle(position, rect))
                {
                    rect.Fill = new SolidColorBrush(BackgroundColor);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error resetting pixel at position: ({X}, {Y})", position.X, position.Y);
        }
    }

    private static bool IsPointInRectangle(Point point, Rectangle rect)
    {
        var left = Canvas.GetLeft(rect);
        var top = Canvas.GetTop(rect);
        var right = left + rect.Width;
        var bottom = top + rect.Height;

        return point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom;
    }
}