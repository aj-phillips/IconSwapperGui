using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands.IconConverter;

public class RefreshIconsCommand : RelayCommand
{
    private readonly IconConverterViewModel _viewModel;

    public RefreshIconsCommand(IconConverterViewModel viewModel, Action<object> execute, Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        _viewModel.Icons.Clear();
        _viewModel.PopulateIconsList(_viewModel.IconsFolderPath);
    }
}