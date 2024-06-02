using IconSwapperGui.ViewModels;
using Microsoft.Win32;

namespace IconSwapperGui.Commands;

public class ChooseIconFolderCommand : RelayCommand
{
    private readonly MainViewModel _viewModel;

    public ChooseIconFolderCommand(MainViewModel viewModel, Action<object> execute,
        Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        _viewModel.Icons.Clear();

        var openFolderDialog = new OpenFolderDialog();

        if (openFolderDialog.ShowDialog() != true) return;

        var folderPath = openFolderDialog.FolderName;

        _viewModel.IconsFolderPath = folderPath;

        _viewModel.PopulateIconsList(folderPath);

        _viewModel.SettingsService.SaveIconsLocation(_viewModel.IconsFolderPath);
    }
}