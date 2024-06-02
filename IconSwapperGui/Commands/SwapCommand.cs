using System.IO;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Commands;

public class SwapCommand : RelayCommand
{
    private readonly MainViewModel _viewModel;
    public SwapCommand(MainViewModel viewModel, Action<object> execute, Func<object, bool>? canExecute = null) 
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        if (_viewModel.SelectedApplication == null || _viewModel.SelectedIcon == null)
        {
            _viewModel.DialogService.ShowWarning("Please select an application and an icon to swap.", "No Application or Icon Selected");
            return;
        }

        try
        {
            var extension = Path.GetExtension(_viewModel.SelectedApplication.Path).ToLower();

            switch (extension)
            {
                case ".lnk":
                    SwapLinkFileIcon();
                    break;
                case ".url":
                    SwapUrlFileIcon();
                    break;
            }

            _viewModel.DialogService.ShowInformation($"The icon for {_viewModel.SelectedApplication.Name} has been successfully swapped.", "Icon Swapped");
            _viewModel.ResetGui();
        }
        catch (Exception ex)
        {
            _viewModel.DialogService.ShowError($"An error occurred while swapping the icon for {_viewModel.SelectedApplication.Name}: {ex.Message}",
                "Error Swapping Icon");
        }
    }

    private void SwapLinkFileIcon()
    {
        const string publicDesktopPath = "C:\\Users\\Public\\Desktop";

        if (Path.GetDirectoryName(_viewModel.SelectedApplication.Path).Equals(publicDesktopPath) && !_viewModel.ElevationService.IsRunningAsAdministrator())
        {
            _viewModel.DialogService.ShowInformation(
                $"To change the icon of {_viewModel.SelectedApplication.Name}, the application needs to be restarted with administrator permissions.\n\nYou will need to attempt the swap again afterwards",
                "Permissions Required To Swap Icon");

            _viewModel.ElevationService.ElevateApplicationViaUac();
        }

        var wshShell = (IWshRuntimeLibrary.WshShell)Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));

        var shortcut =
            (IWshRuntimeLibrary.IWshShortcut)wshShell.CreateShortcut(_viewModel.SelectedApplication.Path);

        shortcut.IconLocation = $"{_viewModel.SelectedIcon.Path},0";

        shortcut.Save();
    }

    private void SwapUrlFileIcon()
    {
        var urlFileContent = File.ReadAllLines(_viewModel.SelectedApplication.Path);

        for (var i = 0; i < urlFileContent.Length; i++)
        {
            if (urlFileContent[i].StartsWith("IconFile", StringComparison.CurrentCultureIgnoreCase))
            {
                urlFileContent[i] = "IconFile=" + _viewModel.SelectedIcon.Path;
            }
        }

        File.WriteAllLines(_viewModel.SelectedApplication.Path, urlFileContent);
    }
}