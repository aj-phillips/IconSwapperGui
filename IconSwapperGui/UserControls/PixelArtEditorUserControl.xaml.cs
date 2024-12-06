using System.Windows;
using System.Windows.Input;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls;

public partial class PixelArtEditorUserControl
{
    private PixelArtEditorViewModel? _viewModel;

    public PixelArtEditorUserControl()
    {
        InitializeComponent();
        DataContext = new PixelArtEditorViewModel();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as PixelArtEditorViewModel;
        if (_viewModel == null) return;

        _viewModel.DrawableCanvas = DrawableCanvas;

        _viewModel.ApplyLayoutCommand.Execute(null);
    }

    private void HandleEvent(EventArgs e, ICommand command)
    {
        if (command.CanExecute(e)) command.Execute(e);
    }

    private void DrawableCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel?.DrawableCanvasMouseLeftButtonDownCommand != null)
            HandleEvent(e, _viewModel.DrawableCanvasMouseLeftButtonDownCommand);
    }

    private void DrawableCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel?.DrawableCanvasMouseMoveCommand != null)
            HandleEvent(e, _viewModel.DrawableCanvasMouseMoveCommand);
    }

    private void DrawableCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel?.DrawableCanvasMouseRightButtonDownCommand != null)
            HandleEvent(e, _viewModel.DrawableCanvasMouseRightButtonDownCommand);
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_viewModel?.ZoomSliderValueChangedCommand != null)
            HandleEvent(e, _viewModel.ZoomSliderValueChangedCommand);
    }
}