using System.Diagnostics;
using System.IO;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands.Swapper.ContextMenu;

public class OpenExplorerContextCommand : RelayCommand
{
    private readonly SwapperViewModel _viewModel;

    public OpenExplorerContextCommand(SwapperViewModel viewModel, Action<object> execute = null,
        Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        if (_viewModel.SelectedIcon is not null && File.Exists(_viewModel.SelectedIcon.Path))
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select, \"{_viewModel.SelectedIcon.Path}\"")
                { UseShellExecute = true });
    }
}