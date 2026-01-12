using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IconSwapperGui.Core.PixelArt;
using IconSwapperGui.UI.Services.PixelArtEditor;
using IconSwapperGui.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IconSwapperGui.UI.Views
{
    /// <summary>
    /// Interaction logic for PixelArtEditorView.xaml
    /// </summary>
    public partial class PixelArtEditorView : UserControl
    {
        private PixelArtRenderService? _renderer;
        private PixelArtEditorViewModel? _vm;
        private bool _isPainting;

        public PixelArtEditorView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as PixelArtEditorViewModel;
            if (_vm is null)
            {
                // When resolved via NavigationService, DataContext may be set later.
                DataContextChanged += OnDataContextChanged;
                return;
            }

            Attach(_vm);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Detach();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is PixelArtEditorViewModel vm)
            {
                DataContextChanged -= OnDataContextChanged;
                _vm = vm;
                Attach(vm);
            }
        }

        private void Attach(PixelArtEditorViewModel vm)
        {
            _renderer = ((App)Application.Current).Services.GetRequiredService<PixelArtRenderService>();
            _renderer.Attach(DrawableCanvas);

            vm.LayoutInvalidated += OnLayoutInvalidated;
            vm.PixelChanged += OnPixelChanged;

            DrawableCanvas.MouseLeftButtonDown += OnCanvasMouseLeftButtonDown;
            DrawableCanvas.MouseMove += OnCanvasMouseMove;
            DrawableCanvas.MouseLeftButtonUp += OnCanvasMouseLeftButtonUp;

            DrawableCanvas.MouseRightButtonDown += OnCanvasMouseRightButtonDown;
            DrawableCanvas.MouseRightButtonUp += OnCanvasMouseRightButtonUp;

            DrawableCanvas.Focusable = true;
            DrawableCanvas.Focus();

            vm.ApplyLayoutCommand.Execute(null);
        }

        private void Detach()
        {
            if (_vm is not null)
            {
                _vm.LayoutInvalidated -= OnLayoutInvalidated;
                _vm.PixelChanged -= OnPixelChanged;
            }

            DrawableCanvas.MouseLeftButtonDown -= OnCanvasMouseLeftButtonDown;
            DrawableCanvas.MouseMove -= OnCanvasMouseMove;
            DrawableCanvas.MouseLeftButtonUp -= OnCanvasMouseLeftButtonUp;
            DrawableCanvas.MouseRightButtonDown -= OnCanvasMouseRightButtonDown;
            DrawableCanvas.MouseRightButtonUp -= OnCanvasMouseRightButtonUp;

            _isPainting = false;
        }

        private void OnLayoutInvalidated()
        {
            if (_vm is null || _renderer is null) return;

            _renderer.Initialize(_vm.Rows, _vm.Columns, _vm.BackgroundColor, _vm.IsGridVisible);
            _renderer.Redraw(_vm.PixelsArgb);
        }

        private void OnPixelChanged(int index, System.Windows.Media.Color color)
        {
            _renderer?.SetCell(index, color);
        }

        private void OnCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm is null || _renderer is null) return;
            DrawableCanvas.CaptureMouse();
            _isPainting = true;

            if (_renderer.TryGetCellIndex(e.GetPosition(DrawableCanvas), out var index))
            {
                _vm.BeginStroke(index);
            }
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPainting || _vm is null || _renderer is null) return;

            if (_renderer.TryGetCellIndex(e.GetPosition(DrawableCanvas), out var index))
            {
                _vm.ContinueStroke(index);
            }
        }

        private void OnCanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_vm is null) return;
            _isPainting = false;
            DrawableCanvas.ReleaseMouseCapture();
            _vm.EndStroke();
        }

        private void OnCanvasMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm is null) return;

            // Temporary affordance: right-click acts like eraser.
            var previous = _vm.SelectedTool;
            _vm.SelectedTool = PixelTool.Eraser;
            OnCanvasMouseLeftButtonDown(sender, e);
            _vm.SelectedTool = previous;
        }

        private void OnCanvasMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            OnCanvasMouseLeftButtonUp(sender, e);
        }

        private void SelectedColorPicker_ColorPicked(object sender, System.EventArgs e)
        {
            // Intentionally no-op: the picker is configured to stay open until the user clicks away.
        }

        private void SelectedColorPicker_ColorCommitted(object sender, System.EventArgs e)
        {
            if (_vm is null) return;
            _vm.CommitRecentColor(_vm.SelectedColor);
        }

        private void BackgroundColorPicker_ColorPicked(object sender, System.EventArgs e)
        {
            // Intentionally no-op: the picker is configured to stay open until the user clicks away.
        }

        private void BackgroundColorPicker_ColorCommitted(object sender, System.EventArgs e)
        {
            if (_vm is null) return;
            _vm.CommitRecentColor(_vm.BackgroundColor);
        }

        private void LayerNameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            tb.Focus();
            tb.SelectAll();
        }

        private void LayerNameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (tb.DataContext is PixelArtLayerViewModel layer)
            {
                layer.IsRenaming = false;
            }
        }

        private void LayerNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (tb.DataContext is not PixelArtLayerViewModel layer) return;

            if (e.Key == Key.Enter)
            {
                layer.IsRenaming = false;
                e.Handled = true;
                Keyboard.ClearFocus();
            }
            else if (e.Key == Key.Escape)
            {
                layer.IsRenaming = false;
                e.Handled = true;
                Keyboard.ClearFocus();
            }
        }

        private void DocumentNameTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            tb.Focus();
            tb.SelectAll();
        }

        private void DocumentNameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_vm is null) return;
            _vm.IsRenamingDocumentName = false;
        }

        private void DocumentNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (_vm is null) return;

            if (e.Key == Key.Enter)
            {
                _vm.IsRenamingDocumentName = false;
                e.Handled = true;
                Keyboard.ClearFocus();
            }
            else if (e.Key == Key.Escape)
            {
                _vm.IsRenamingDocumentName = false;
                e.Handled = true;
                Keyboard.ClearFocus();
            }
        }

        private void RootGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm is null) return;
            if (!_vm.IsRenamingDocumentName) return;

            if (sender is not DependencyObject root) return;

            var clicked = e.OriginalSource as DependencyObject;
            if (clicked is null)
            {
                Keyboard.ClearFocus();
                return;
            }

            // If click is inside the document name editor, don't clear focus.
            var insideEditor = false;
            var current = clicked;
            while (current is not null)
            {
                if (current is TextBox tb && tb.Name == "DocumentNameEditor")
                {
                    insideEditor = true;
                    break;
                }

                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            if (!insideEditor)
            {
                Keyboard.ClearFocus();
            }
        }

        private void RenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (e.NewValue is not bool isVisible || !isVisible) return;

            tb.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                tb.Focus();
                tb.SelectAll();
            }));
        }

        private void RenameTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            tb.SelectAll();
        }
    }
}
