using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IconSwapperGui.UI.Services.PixelArtEditor;

public sealed class PixelArtRenderService
{
    private Canvas? _canvas;
    private Rectangle[] _cells = Array.Empty<Rectangle>();
    private Rectangle? _backgroundRect;
    private int _rows;
    private int _columns;
    private double _cellSize;
    private bool _useTransparentBackground;

    public void Attach(Canvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        _canvas = canvas;
    }

    public void Initialize(int rows, int columns, Color background, bool showGrid, bool useTransparentBackground)
    {
        if (_canvas is null) throw new InvalidOperationException("Renderer is not attached to a Canvas.");
        if (rows <= 0) throw new ArgumentOutOfRangeException(nameof(rows));
        if (columns <= 0) throw new ArgumentOutOfRangeException(nameof(columns));

        _rows = rows;
        _columns = columns;
        _useTransparentBackground = useTransparentBackground;

        _canvas.Children.Clear();

        var width = _canvas.Width;
        var height = _canvas.Height;
        if (double.IsNaN(width) || width <= 0) width = 512;
        if (double.IsNaN(height) || height <= 0) height = 512;

        _cellSize = Math.Floor(Math.Min(width / columns, height / rows));
        if (_cellSize <= 0) _cellSize = 1;

        _canvas.Width = _cellSize * columns;
        _canvas.Height = _cellSize * rows;

        // Add background rectangle for checkerboard or solid color
        _backgroundRect = new Rectangle
        {
            Width = _canvas.Width,
            Height = _canvas.Height,
            Fill = CreateBackgroundBrush(background, useTransparentBackground)
        };
        Canvas.SetLeft(_backgroundRect, 0);
        Canvas.SetTop(_backgroundRect, 0);
        _canvas.Children.Add(_backgroundRect);

        var stroke = showGrid ? new SolidColorBrush(Color.FromArgb(0x40, 0x00, 0x00, 0x00)) : null;
        var strokeThickness = showGrid ? 0.5 : 0;

        var fillBrush = new SolidColorBrush(Colors.Transparent);
        fillBrush.Freeze();

        _cells = new Rectangle[checked(rows * columns)];

        var index = 0;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < columns; c++)
            {
                var rect = new Rectangle
                {
                    Width = _cellSize,
                    Height = _cellSize,
                    Fill = fillBrush,
                    Stroke = stroke,
                    StrokeThickness = strokeThickness,
                    SnapsToDevicePixels = true
                };

                Canvas.SetLeft(rect, c * _cellSize);
                Canvas.SetTop(rect, r * _cellSize);

                _cells[index++] = rect;
                _canvas.Children.Add(rect);
            }
        }
    }

    public void UpdateBackground(Color background, bool useTransparentBackground)
    {
        if (_backgroundRect is null) return;
        _useTransparentBackground = useTransparentBackground;
        _backgroundRect.Fill = CreateBackgroundBrush(background, useTransparentBackground);
    }

    private Brush CreateBackgroundBrush(Color background, bool useTransparentBackground)
    {
        if (useTransparentBackground)
        {
            // Create checkerboard pattern
            var checkerSize = Math.Max(4.0, _cellSize / 4.0);
            var drawingBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, checkerSize * 2, checkerSize * 2),
                ViewportUnits = BrushMappingMode.Absolute,
                Drawing = new DrawingGroup
                {
                    Children =
                    {
                        new GeometryDrawing(new SolidColorBrush(Color.FromRgb(204, 204, 204)), null, new RectangleGeometry(new Rect(0, 0, checkerSize, checkerSize))),
                        new GeometryDrawing(new SolidColorBrush(Color.FromRgb(204, 204, 204)), null, new RectangleGeometry(new Rect(checkerSize, checkerSize, checkerSize, checkerSize))),
                        new GeometryDrawing(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new RectangleGeometry(new Rect(checkerSize, 0, checkerSize, checkerSize))),
                        new GeometryDrawing(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new RectangleGeometry(new Rect(0, checkerSize, checkerSize, checkerSize)))
                    }
                }
            };
            drawingBrush.Freeze();
            return drawingBrush;
        }
        else
        {
            var brush = new SolidColorBrush(background);
            brush.Freeze();
            return brush;
        }
    }

    public void SetCell(int index, Color color)
    {
        if (_canvas is null) return;
        if ((uint)index >= (uint)_cells.Length) return;

        var brush = new SolidColorBrush(color);
        brush.Freeze();
        _cells[index].Fill = brush;
    }

    public void Redraw(ReadOnlySpan<uint> argb)
    {
        if (_canvas is null) return;
        if (argb.Length != _cells.Length) return;

        for (var i = 0; i < argb.Length; i++)
        {
            var color = FromArgb(argb[i]);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            _cells[i].Fill = brush;
        }
    }

    public bool TryGetCellIndex(Point point, out int index)
    {
        index = -1;
        if (_canvas is null) return false;
        if (_rows <= 0 || _columns <= 0) return false;
        if (_cellSize <= 0) return false;

        var col = (int)(point.X / _cellSize);
        var row = (int)(point.Y / _cellSize);

        if (col < 0 || col >= _columns) return false;
        if (row < 0 || row >= _rows) return false;

        index = row * _columns + col;
        return true;
    }

    private static Color FromArgb(uint argb)
    {
        var a = (byte)((argb >> 24) & 0xFF);
        var r = (byte)((argb >> 16) & 0xFF);
        var g = (byte)((argb >> 8) & 0xFF);
        var b = (byte)(argb & 0xFF);
        return Color.FromArgb(a, r, g, b);
    }
}
