using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.PixelArtEditor;

namespace IconSwapperGui.ViewModels;

public partial class PixelArtEditorViewModel : ObservableObject
{
    [ObservableProperty] private Color _backgroundColor = Colors.White;

    [ObservableProperty] private int _columns = 32;

    [ObservableProperty] private Canvas? _drawableCanvas;

    [ObservableProperty] private int _rows = 32;

    [ObservableProperty] private Color _selectedColor = Colors.Black;

    [ObservableProperty] private double _zoomLevel = 1.0;

    public PixelArtEditorViewModel()
    {
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
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (DrawableCanvas == null || !PerformMaxSizeValidation(Rows, Columns))
            return;

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

        for (var row = 0; row < Rows; row++)
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

    private bool PerformMaxSizeValidation(int rows, int columns)
    {
        const int maxRows = 96;
        const int maxColumns = 96;

        if (rows <= maxRows && columns <= maxColumns) return true;

        var message = $"The grid size exceeds the maximum allowed values.\n\n" +
                      $"Maximum Rows: {maxRows}\nMaximum Columns: {maxColumns}";

        MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);

        return false;
    }

    private void DrawableCanvas_MouseLeftButtonDown(MouseButtonEventArgs? e)
    {
        if (e == null) return;

        var position = e.GetPosition(DrawableCanvas);
        DrawPixel(position);
    }

    private void DrawableCanvas_MouseMove(MouseEventArgs? e)
    {
        if (e == null) return;

        var position = e.GetPosition(DrawableCanvas);

        if (e.LeftButton == MouseButtonState.Pressed)
            DrawPixel(position);
        else if (e.RightButton == MouseButtonState.Pressed) ResetPixel(position);
    }

    private void DrawableCanvas_MouseRightButtonDown(MouseButtonEventArgs? e)
    {
        if (e == null) return;

        var position = e.GetPosition(DrawableCanvas);

        ResetPixel(position);
    }

    private void ZoomSlider_ValueChanged(RoutedPropertyChangedEventArgs<double> e)
    {
        ZoomLevel = e.NewValue;
    }

    private void DrawPixel(Point position)
    {
        foreach (var rect in Pixels)
            if (IsPointInRectangle(position, rect))
            {
                rect.Fill = new SolidColorBrush(SelectedColor);
                break;
            }
    }

    private void ResetPixel(Point position)
    {
        foreach (var rect in Pixels)
            if (IsPointInRectangle(position, rect))
            {
                rect.Fill = new SolidColorBrush(BackgroundColor);
                break;
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