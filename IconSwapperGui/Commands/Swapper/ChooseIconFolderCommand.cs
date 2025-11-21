using IconSwapperGui.ViewModels;
using IconSwapperGui.ViewModels.Interfaces;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace IconSwapperGui.Commands.Swapper;

public class ChooseIconFolderCommand<TViewModel> : RelayCommand where TViewModel : IIconViewModel
{
    private readonly TViewModel _viewModel;

    public ChooseIconFolderCommand(TViewModel viewModel, Action<object> execute,
        Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    public override void Execute(object? parameter)
    {
        switch (_viewModel)
        {
            case SwapperViewModel iconSwapperViewModel:
                ExecuteCommand(iconSwapperViewModel);
                iconSwapperViewModel.SettingsService.SaveIconsLocation(iconSwapperViewModel.IconsFolderPath);
                iconSwapperViewModel.RefreshGui();
                break;
            case ConverterViewModel iconConverterViewModel:
                ExecuteCommand(iconConverterViewModel);
                iconConverterViewModel.SettingsService.SaveConverterIconsLocation(
                    iconConverterViewModel.IconsFolderPath);
                break;
        }
    }

    private void ExecuteCommand(IIconViewModel viewModel)
    {
        if (viewModel.Icons.Count > 0) viewModel.Icons.Clear();

        var openFolderDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true
        };

        if (openFolderDialog.ShowDialog() != CommonFileDialogResult.Ok) return;

        var folderPath = openFolderDialog.FileName;
        viewModel.IconsFolderPath = folderPath!;

        viewModel.PopulateIconsList(folderPath!);
    }
}