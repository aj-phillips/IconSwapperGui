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

        private void HandleMouseEvent(MouseEventArgs e, ICommand command)
        {
            if (command != null && command.CanExecute(e))
            {
                command.Execute(e);
            }
        }

        private void DrawableCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HandleMouseEvent(e, _viewModel?.DrawableCanvasMouseLeftButtonDownCommand);
        }

        private void DrawableCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            HandleMouseEvent(e, _viewModel?.DrawableCanvasMouseMoveCommand);
        }

        private void DrawableCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            HandleMouseEvent(e, _viewModel?.DrawableCanvasMouseRightButtonDownCommand);
        }
    }
}