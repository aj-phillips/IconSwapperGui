using IconSwapperGui.ViewModels;
using IconSwapperGui.Services.Interfaces;

namespace IconSwapperGui.Commands.Swapper;

public class SwapFolderIconCommand : RelayCommand
{
    private readonly SwapperViewModel _viewModel;
    private readonly IFolderService _folderService;
    private readonly IIconHistoryService? _historyService;

    public SwapFolderIconCommand(SwapperViewModel viewModel, IFolderService folderService, IIconHistoryService? historyService = null, Action<object> execute = null!, Func<object, bool>? canExecute = null) : base(execute, canExecute)
    {
        _viewModel = viewModel;
        _folderService = folderService ?? throw new ArgumentNullException(nameof(folderService));
        _historyService = historyService;
    }

    public override async void Execute(object? parameter)
    {
        if (_viewModel.SelectedFolder == null || _viewModel.SelectedIcon == null)
        {
            _viewModel.DialogService.ShowWarning("Please select a folder and an icon to swap.", "No Folder or Icon Selected");
            return;
        }

        var success = await _folderService.ChangeFolderIconAsync(_viewModel.SelectedFolder.Path, _viewModel.SelectedIcon.Path);

        if (success)
        {
            try
            {
                if (_historyService != null)
                {
                    await _historyService.RecordIconChangeAsync(_viewModel.SelectedFolder.Path,
                        _viewModel.SelectedIcon.Path);
                }
            }
            catch
            {
                // ignore
            }

            await _viewModel.ShowSuccessTick();
            _viewModel.RefreshGui();
        }
        else
        {
            _viewModel.DialogService.ShowError("Error", "Failed to change folder icon");
        }
    }
}
