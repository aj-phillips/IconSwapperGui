using IconSwapperGui.Interfaces;
using IconSwapperGui.ViewModels;
using Microsoft.Win32;

namespace IconSwapperGui.Commands.Swapper;

public class ChooseIconFolderCommand<TViewModel> : RelayCommand where TViewModel : IIconViewModel
{
    private readonly TViewModel _viewModel;

    public ChooseIconFolderCommand(TViewModel viewModel, Action<object> execute,
        Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
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
            iconConverterViewModel.SettingsService.SaveConverterIconsLocation(iconConverterViewModel.IconsFolderPath);
        }
    }
    
    private void ExecuteCommand(IIconViewModel viewModel)
    {
        viewModel.Icons.Clear();
        var openFolderDialog = new OpenFolderDialog();
        if (openFolderDialog.ShowDialog() != true) return;
        var folderPath = openFolderDialog.FolderName;
        viewModel.IconsFolderPath = folderPath;
        viewModel.PopulateIconsList(folderPath);
    }
}