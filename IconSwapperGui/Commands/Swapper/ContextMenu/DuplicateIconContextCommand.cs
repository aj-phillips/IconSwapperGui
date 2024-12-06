using System.IO;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands.Swapper.ContextMenu;

public class DuplicateIconContextCommand : RelayCommand
{
    private readonly SwapperViewModel _viewModel;

    public DuplicateIconContextCommand(SwapperViewModel viewModel, Action<object> execute = null!,
        Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        if (_viewModel.SelectedIcon is null)
            return;

        var directory = Path.GetDirectoryName(_viewModel.SelectedIcon.Path);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(_viewModel.SelectedIcon.Path);
        var extension = Path.GetExtension(_viewModel.SelectedIcon.Path);
        var newFileName = $"{fileNameWithoutExtension} - Copy{extension}";
        var newFilePath = Path.Combine(directory!, newFileName);

        var count = 1;

        while (File.Exists(newFilePath))
        {
            newFileName = $"{fileNameWithoutExtension} - Copy ({count++}){extension}";
            newFilePath = Path.Combine(directory!, newFileName);
        }

        File.Copy(_viewModel.SelectedIcon.Path, newFilePath);

        _viewModel.PopulateIconsList(_viewModel.IconsFolderPath);
    }
}