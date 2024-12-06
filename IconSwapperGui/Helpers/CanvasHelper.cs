using System.Windows;
using System.Windows.Controls;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Helpers;

public static class CanvasHelper
{
    public static readonly DependencyProperty CanvasProperty =
        DependencyProperty.RegisterAttached("Canvas",
            typeof(Canvas),
            typeof(CanvasHelper),
            new PropertyMetadata(null, OnCanvasChanged));

    public static Canvas GetCanvas(DependencyObject obj)
    {
        return (Canvas)obj.GetValue(CanvasProperty);
    }

    public static void SetCanvas(DependencyObject obj, Canvas value)
    {
        obj.SetValue(CanvasProperty, value);
    }

    private static void OnCanvasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element || e.NewValue is not Canvas canvas) return;

        if (element.DataContext is PixelArtEditorViewModel viewModel) viewModel.DrawableCanvas = canvas;
    }

    public static void GetDpi(Canvas? canvas, out double dpiX, out double dpiY)
    {
        var source = PresentationSource.FromVisual(canvas!);

        if (source?.CompositionTarget != null)
        {
            var transformToDevice = source.CompositionTarget.TransformToDevice;
            dpiX = transformToDevice.M11 * 96;
            dpiY = transformToDevice.M22 * 96;
        }
        else
        {
            dpiX = 96;
            dpiY = 96;
        }
    }
}