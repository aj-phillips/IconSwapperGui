using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands.IconSwapper;

public class RefreshCommand : RelayCommand
{
    private readonly IconSwapperViewModel _viewModel;

    public RefreshCommand(IconSwapperViewModel viewModel, Action<object> execute, Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        _viewModel.Applications.Clear();
        _viewModel.Icons.Clear();

        _viewModel.PopulateApplicationsList(_viewModel.ApplicationsFolderPath);
        _viewModel.PopulateIconsList(_viewModel.IconsFolderPath);
    }
}