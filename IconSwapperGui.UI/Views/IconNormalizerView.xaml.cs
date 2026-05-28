using System.Windows;
using System.Windows.Controls;
using IconSwapperGui.UI.ViewModels;

namespace IconSwapperGui.UI.Views;

public partial class IconNormalizerView : UserControl
{
    public IconNormalizerView()
    {
        InitializeComponent();
    }

    private void IconNormalizer_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void IconNormalizer_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is IconNormalizerViewModel vm && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            vm.AddPaths(paths);

        e.Handled = true;
    }
}
