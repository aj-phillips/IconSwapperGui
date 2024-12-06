using System.IO;
using System.Windows;
using IconSwapperGui.ViewModels;
using Application = System.Windows.Application;

namespace IconSwapperGui.Commands.Swapper.ContextMenu;

public class DeleteIconContextCommand : RelayCommand
{
    private readonly SwapperViewModel _viewModel;

    public DeleteIconContextCommand(SwapperViewModel viewModel, Action<object> execute = null,
        Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        var iconToDelete = _viewModel.SelectedIcon;
        if (iconToDelete == null || !File.Exists(iconToDelete.Path)) return;

        try
        {
            File.Delete(iconToDelete.Path);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Application.Current.Dispatcher.Invoke(() => { _viewModel.Icons.Remove(iconToDelete); });

        _viewModel.FilterIcons();
        _viewModel.PopulateIconsList(_viewModel.IconsFolderPath);
    }
}