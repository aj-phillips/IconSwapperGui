using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using IconSwapperGui.Commands;
using IconSwapperGui.Commands.PixelArtEditor;

namespace IconSwapperGui.ViewModels;

public class PixelArtEditorViewModel : ViewModel
{
    public ObservableCollection<Rectangle> Pixels { get; private set; }
    public ICommand ApplyLayoutCommand { get; private set; }
    public ICommand DrawableCanvasMouseLeftButtonDownCommand { get; private set; }
    public ICommand DrawableCanvasMouseMoveCommand { get; private set; }
    public ICommand DrawableCanvasMouseRightButtonDownCommand { get; private set; }
    public ICommand ZoomSliderValueChangedCommand { get; private set; }
    public RelayCommand ExportIconCommand { get; }
    
    private int _rows = 32;

    public int Rows
    {
        get => _rows;
        set
        {
            if (_rows != value)
            {
                _rows = value;
                OnPropertyChanged();
            }
        }
    }

    private int _columns = 32;

    public int Columns
    {
        get => _columns;
        set
        {
            if (_columns != value)
            {
                _columns = value;
                OnPropertyChanged();
            }
        }
    }

    private double _zoomLevel = 1.0;
    
    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (_zoomLevel != value)
            {
                _zoomLevel = value;
                OnPropertyChanged();
            }
        }
    }
    
    private Color _backgroundColor = Colors.White;

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            if (_backgroundColor != value)
            {
                _backgroundColor = value;
                OnPropertyChanged();
            }
        }
    }

    private Color _selectedColor = Colors.Black;

    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            if (_selectedColor != value)
            {
                _selectedColor = value;
                OnPropertyChanged();
            }
        }
    }

    private Canvas _drawableCanvas;

    public Canvas DrawableCanvas
    {
        get => _drawableCanvas;
        set
        {
            if (_drawableCanvas != value)
            {
                _drawableCanvas = value;
                OnPropertyChanged();
                ApplyLayout();
            }
        }
    }

    public PixelArtEditorViewModel()
    {
        Pixels = new ObservableCollection<Rectangle>();
        ApplyLayoutCommand = new RelayCommand(_ => ApplyLayout());
        DrawableCanvasMouseLeftButtonDownCommand = new RelayCommand(e => DrawableCanvas_MouseLeftButtonDown((MouseButtonEventArgs)e));
        DrawableCanvasMouseMoveCommand = new RelayCommand(e => DrawableCanvas_MouseMove((MouseEventArgs)e));
        DrawableCanvasMouseRightButtonDownCommand =
            new RelayCommand(e => DrawableCanvas_MouseRightButtonDown((MouseButtonEventArgs)e));
        ZoomSliderValueChangedCommand = new RelayCommand(e => ZoomSlider_ValueChanged((RoutedPropertyChangedEventArgs<double>)e));
        ExportIconCommand = new ExportIconCommand(this, null!, x => true);
    }

    public void ApplyLayout()
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
    }
    
    public bool PerformMaxSizeValidation(int rows, int columns)
    {
        const int maxRows = 96;
        const int maxColumns = 96;

        if (rows > maxRows || columns > maxColumns)
        {
            var message = $"The grid size exceeds the maximum allowed values.\n\n" +
                          $"Maximum Rows: {maxRows}\nMaximum Columns: {maxColumns}";
            MessageBox.Show(message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    public void DrawableCanvas_MouseLeftButtonDown(MouseButtonEventArgs? e)
    {
        if (e != null)
        {
            var position = e.GetPosition(DrawableCanvas);
            DrawPixel(position);
        }
    }

    public void DrawableCanvas_MouseMove(MouseEventArgs? e)
    {
        var position = e.GetPosition(DrawableCanvas);
        
        if (e?.LeftButton == MouseButtonState.Pressed && e != null)
        {
            DrawPixel(position);
        }
        else if (e?.RightButton == MouseButtonState.Pressed && e != null)
        {
            ResetPixel(position);
        }
    }

    public void DrawableCanvas_MouseRightButtonDown(MouseButtonEventArgs? e)
    {
        if (e != null)
        {
            var position = e.GetPosition(DrawableCanvas);
            ResetPixel(position);
        }
    }
    
    private void ZoomSlider_ValueChanged(RoutedPropertyChangedEventArgs<double> e)
    {
        if (e != null)
        {
            ZoomLevel = e.NewValue;
        }
    }

    public void DrawPixel(Point position)
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

    public void ResetPixel(Point position)
    {
        foreach (var rect in Pixels)
        {
            if (IsPointInRectangle(position, rect))
            {
                rect.Fill = Brushes.White;
                break;
            }
        }
    }

    private bool IsPointInRectangle(Point point, Rectangle rect)
    {
        var left = Canvas.GetLeft(rect);
        var top = Canvas.GetTop(rect);
        var right = left + rect.Width;
        var bottom = top + rect.Height;

        return (point.X >= left && point.X <= right && point.Y >= top && point.Y <= bottom);
    }
}