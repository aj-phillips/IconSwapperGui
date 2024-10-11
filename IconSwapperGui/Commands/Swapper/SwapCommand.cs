using System;
using System.IO;
using IconSwapperGui.Helpers;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands.Swapper
{
    public class SwapCommand : RelayCommand
    {
        private readonly SwapperViewModel _viewModel;
        private readonly LnkIconSwapper _lnkIconSwapper;
        private readonly UrlIconSwapper _urlIconSwapper;

        public SwapCommand(SwapperViewModel viewModel, Action<object> execute, Func<object, bool>? canExecute = null)
            : base(execute, canExecute)
        {
            _viewModel = viewModel;
            _lnkIconSwapper = new LnkIconSwapper(viewModel.DialogService, viewModel.ElevationService);
            _urlIconSwapper = new UrlIconSwapper();
        }

        public override void Execute(object? parameter)
        {
            if (_viewModel.SelectedApplication == null || _viewModel.SelectedIcon == null)
            {
                _viewModel.DialogService.ShowWarning("Please select an application and an icon to swap.",
                    "No Application or Icon Selected");
                return;
            }

            try
            {
                var extension = Path.GetExtension(_viewModel.SelectedApplication.Path).ToLower();

                switch (extension)
                {
                    case ".lnk":
                        _lnkIconSwapper.Swap(_viewModel.SelectedApplication.Path, _viewModel.SelectedIcon.Path,
                            _viewModel.SelectedApplication.Name);
                        break;
                    case ".url":
                        _urlIconSwapper.Swap(_viewModel.SelectedApplication.Path, _viewModel.SelectedIcon.Path);
                        break;
                }

                _viewModel.DialogService.ShowInformation(
                    $"The icon for {_viewModel.SelectedApplication.Name} has been successfully swapped.",
                    "Icon Swapped");
                _viewModel.ResetGui();
            }
            catch (Exception ex)
            {
                _viewModel.DialogService.ShowError(
                    $"An error occurred while swapping the icon for {_viewModel.SelectedApplication.Name}: {ex.Message}",
                    "Error Swapping Icon");
            }
        }
    }
}