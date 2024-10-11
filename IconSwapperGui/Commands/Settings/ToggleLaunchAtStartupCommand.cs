using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands.Settings;

public class ToggleLaunchAtStartupCommand : RelayCommand
{
    private readonly SettingsViewModel _viewModel;

    public ToggleLaunchAtStartupCommand(SettingsViewModel viewModel, Action<object> execute,
        Func<object, bool>? canExecute = null) : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        _viewModel.ToggleLaunchAtStartup();
    }
}