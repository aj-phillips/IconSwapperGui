using System.Windows;
using System.Windows.Input;
using IconSwapperGui.UI.ViewModels;

namespace IconSwapperGui.UI.Windows;

public partial class ImageCropEditorWindow : Window
{
    private Point? _lastDragPoint;

    public ImageCropEditorWindow()
    {
        InitializeComponent();
    }

    private void CropPreviewSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastDragPoint = e.GetPosition(CropPreviewSurface);
        CropPreviewSurface.CaptureMouse();
        e.Handled = true;
    }

    private void CropPreviewSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (_lastDragPoint is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var currentPoint = e.GetPosition(CropPreviewSurface);
        var delta = currentPoint - _lastDragPoint.Value;
        _lastDragPoint = currentPoint;

        if (DataContext is ImageCropEditorViewModel viewModel)
            viewModel.PanFocus(delta.X, delta.Y);

        e.Handled = true;
    }

    private void CropPreviewSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _lastDragPoint = null;
        CropPreviewSurface.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void CropPreviewSurface_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is ImageCropEditorViewModel viewModel)
            viewModel.AdjustZoom(e.Delta);

        e.Handled = true;
    }
}
