using IconSwapperGui.Interfaces;
using IconSwapperGui.ViewModels;
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
        if (_viewModel is SwapperViewModel iconSwapperViewModel)
        {
            ExecuteCommand(iconSwapperViewModel);
            iconSwapperViewModel.SettingsService.SaveIconsLocation(iconSwapperViewModel.IconsFolderPath);
        }
        else if (_viewModel is ConverterViewModel iconConverterViewModel)
        {
            ExecuteCommand(iconConverterViewModel);
            iconConverterViewModel.SettingsService.SaveConverterIconsLocation(
                iconConverterViewModel.IconsFolderPath);
        }
    }

    private void ExecuteCommand(IIconViewModel viewModel)
    {
        if (viewModel.Icons != null && viewModel.Icons.Count > 0) viewModel.Icons.Clear();

        var openFolderDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true
        };

        if (openFolderDialog.ShowDialog() != CommonFileDialogResult.Ok) return;

        var folderPath = openFolderDialog.FileName;
        viewModel.IconsFolderPath = folderPath;

        viewModel.PopulateIconsList(folderPath);
    }
}