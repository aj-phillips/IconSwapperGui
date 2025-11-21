using IconSwapperGui.ViewModels;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace IconSwapperGui.Commands.Swapper;

public class ChooseFoldersFolderCommand : RelayCommand
{
    private readonly SwapperViewModel _viewModel;

    public ChooseFoldersFolderCommand(SwapperViewModel viewModel, Action<object> execute, Func<object, bool>? canExecute = null) : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        var dlg = new CommonOpenFileDialog { IsFolderPicker = true };
        if (dlg.ShowDialog() != CommonFileDialogResult.Ok) return;

        var folder = dlg.FileName;
        _viewModel.FoldersFolderPath = folder;
        _viewModel.PopulateFoldersList(folder);
        _viewModel.SettingsService.SaveFoldersLocation(folder);
    }
}
