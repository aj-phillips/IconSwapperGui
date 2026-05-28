using System.Windows;
using System.Windows.Controls;
using IconSwapperGui.UI.ViewModels;

namespace IconSwapperGui.UI.Views;

/// <summary>
///     Interaction logic for SwapperView.xaml
/// </summary>
public partial class SwapperView : UserControl
{
    public SwapperView()
    {
        InitializeComponent();
    }

    private static bool TryGetDroppedPaths(DragEventArgs e, out string[] paths)
    {
        paths = [];
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return false;

        paths = e.Data.GetData(DataFormats.FileDrop) as string[] ?? [];
        return paths.Length > 0;
    }

    private static void SetDropEffect(DragEventArgs e)
    {
        e.Effects = TryGetDroppedPaths(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void ShortcutSources_DragOver(object sender, DragEventArgs e) => SetDropEffect(e);

    private async void ShortcutSources_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is SwapperViewModel vm && TryGetDroppedPaths(e, out var paths))
            await vm.AddDroppedShortcutPathsAsync(paths);

        e.Handled = true;
    }

    private void FolderSources_DragOver(object sender, DragEventArgs e) => SetDropEffect(e);

    private async void FolderSources_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is SwapperViewModel vm && TryGetDroppedPaths(e, out var paths))
            await vm.AddDroppedFolderPathsAsync(paths);

        e.Handled = true;
    }

    private void IconLibrary_DragOver(object sender, DragEventArgs e) => SetDropEffect(e);

    private async void IconLibrary_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is SwapperViewModel vm && TryGetDroppedPaths(e, out var paths))
            await vm.AddDroppedIconPathsAsync(paths);

        e.Handled = true;
    }
}
