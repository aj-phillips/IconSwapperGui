using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands.Settings;

public class ToggleDarkModeCommand : RelayCommand
{
    private readonly SettingsViewModel _viewModel;

    public ToggleDarkModeCommand(SettingsViewModel viewModel, Action<object> execute,
        Func<object, bool>? canExecute = null) : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        _viewModel.ToggleDarkMode();
    }
}