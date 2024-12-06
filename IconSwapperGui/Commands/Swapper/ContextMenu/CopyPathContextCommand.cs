using System.Windows;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands.Swapper.ContextMenu;

public class CopyPathContextCommand : RelayCommand
{
    private readonly SwapperViewModel _viewModel;

    public CopyPathContextCommand(SwapperViewModel viewModel, Action<object> execute = null!,
        Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        if (_viewModel.SelectedIcon is not null) Clipboard.SetText(_viewModel.SelectedIcon.Path);
    }
}