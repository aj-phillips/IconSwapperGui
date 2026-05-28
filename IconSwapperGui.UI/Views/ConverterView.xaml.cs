using System.Windows;
using System.Windows.Controls;

namespace IconSwapperGui.UI.Views
{
    /// <summary>
    /// Interaction logic for ConverterView.xaml
    /// </summary>
    public partial class ConverterView : UserControl
    {
        public ConverterView()
        {
            InitializeComponent();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModels.ConverterViewModel vm)
            {
                vm.RefreshGui();
            }
        }

        private void Converter_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Converter_Drop(object sender, DragEventArgs e)
        {
            if (DataContext is not ViewModels.ConverterViewModel vm)
                return;

            if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
                return;

            await vm.AddDroppedPathsAsync(paths);
            e.Handled = true;
        }
    }
}
