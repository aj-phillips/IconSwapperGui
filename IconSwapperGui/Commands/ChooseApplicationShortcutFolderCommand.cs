using IconSwapperGui.ViewModels;
using Microsoft.Win32;

namespace IconSwapperGui.Commands;

public class ChooseApplicationShortcutFolderCommand : RelayCommand
{
    private readonly MainViewModel _viewModel;

    public ChooseApplicationShortcutFolderCommand(MainViewModel viewModel, Action<object> execute, Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        var openFolderDialog = new OpenFolderDialog();

        if (openFolderDialog.ShowDialog() != true) return;

        var folderPath = openFolderDialog.FolderName;

        _viewModel.ApplicationsFolderPath = folderPath;

        _viewModel.PopulateApplicationsList(folderPath);

        _viewModel.SettingsService.SaveApplicationsLocation(_viewModel.ApplicationsFolderPath);
    }
}