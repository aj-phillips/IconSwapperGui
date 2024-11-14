using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls
{
    public partial class PixelArtEditorUserControl : UserControl
    {
        private PixelArtEditorViewModel _viewModel;

        public PixelArtEditorUserControl()
        {
            InitializeComponent();
            DataContext = new PixelArtEditorViewModel();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as PixelArtEditorViewModel;
            if (_viewModel != null)
            {
                _viewModel.DrawableCanvas = DrawableCanvas;
                _viewModel.ApplyLayoutCommand.Execute(null);
            }
        }

        private void HandleEvent(EventArgs e, ICommand command)
        {
            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }

        private void DrawableCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleEvent(e, _viewModel?.DrawableCanvasMouseLeftButtonDownCommand);
        }

        private void DrawableCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            HandleEvent(e, _viewModel?.DrawableCanvasMouseMoveCommand);
        }

        private void DrawableCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            HandleEvent(e, _viewModel?.DrawableCanvasMouseRightButtonDownCommand);
        }
        
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            HandleEvent(e, _viewModel?.ZoomSliderValueChangedCommand);
        }
    }
}