using System.IO;
using System.Windows;
using IconSwapperGui.Helpers;
using IconSwapperGui.ViewModels;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ApplicationModel = IconSwapperGui.Models.Application;

namespace IconSwapperGui.Commands.Swapper.ContextMenu;

public class ResetIconContextCommand : RelayCommand
{
    private const string LnkExtension = ".lnk";
    private const string UrlExtension = ".url";
    private const string SteamRegistryPath = @"Software\Valve\Steam";
    private const string SteamPathKey = "SteamPath";
    private readonly LnkIconSwapper _lnkIconSwapper;
    private readonly UrlIconSwapper _urlIconSwapper;
    private readonly SwapperViewModel _viewModel;

    public ResetIconContextCommand(SwapperViewModel viewModel, Action<object> execute = null!,
        Func<object, bool>? canExecute = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel;
        _lnkIconSwapper = new LnkIconSwapper(viewModel.DialogService, viewModel.ElevationService);
        _urlIconSwapper = new UrlIconSwapper();
    }

    public override void Execute(object? parameter)
    {
        var applicationToReset = _viewModel.SelectedApplication;

        if (applicationToReset == null || !File.Exists(applicationToReset.Path))
            return;

        try
        {
            if (ShouldResetAutomatically(applicationToReset))
            {
                ResetIconAutomatically(applicationToReset);
            }
            else
            {
                if (!PromptManualReset())
                    return;

                var manualIconPath = GetManualIconPath(applicationToReset);
                if (manualIconPath == null)
                    return;

                ResetIconManually(applicationToReset, manualIconPath);
            }

            RefreshApplicationsList();
        }
        catch (Exception e)
        {
            ShowErrorMessage(e.Message);
        }
    }

    private static bool ShouldResetAutomatically(ApplicationModel application)
    {
        return application.DefaultTargetPath != null &&
               application.DefaultTargetPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
    }

    private void ResetIconAutomatically(ApplicationModel application)
    {
        var fileExtension = Path.GetExtension(application.Path).ToLower();

        switch (fileExtension)
        {
            case LnkExtension:
                _lnkIconSwapper.Swap(application.Path, application.DefaultTargetPath!, application.Name);
                break;
            case UrlExtension:
                _urlIconSwapper.Swap(application.Path, application.DefaultTargetPath!);
                break;
            default:
                throw new InvalidOperationException("Unsupported file extension for automatic reset.");
        }
    }

    private static bool PromptManualReset()
    {
        var result = MessageBox.Show(
            "Unfortunately, this icon needs to be reset manually.\n\nWould you like to proceed?",
            "Error Resetting Icon Automatically",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    private static string? GetManualIconPath(ApplicationModel application)
    {
        var steamPath = GetSteamPath() + "steam\\games";

        using var chooseIconDialog = new CommonOpenFileDialog();
        chooseIconDialog.Title = "Choose Icon";
        chooseIconDialog.InitialDirectory = GetInitialDirectory(application, steamPath);

        return chooseIconDialog.ShowDialog() == CommonFileDialogResult.Ok
            ? chooseIconDialog.FileName
            : null;
    }

    private void ResetIconManually(ApplicationModel application, string iconPath)
    {
        if (!File.Exists(iconPath))
            return;

        var fileExtension = Path.GetExtension(application.Path).ToLower();

        switch (fileExtension)
        {
            case LnkExtension:
                _lnkIconSwapper.Swap(application.Path, iconPath, application.Name);
                break;
            case UrlExtension:
                _urlIconSwapper.Swap(application.Path, iconPath);
                break;
            default:
                throw new InvalidOperationException("Unsupported file extension for manual reset.");
        }
    }

    private static string? GetSteamPath()
    {
        var steamPath = Registry.CurrentUser.OpenSubKey(SteamRegistryPath)?.GetValue(SteamPathKey)?.ToString();

        return steamPath?.Replace("/", "\\") + (steamPath.EndsWith('\\') ? string.Empty : "\\");
    }

    private static string GetInitialDirectory(ApplicationModel application, string? steamPath)
    {
        if (steamPath != null &&
            application.DefaultTargetPath?.Contains("steam", StringComparison.OrdinalIgnoreCase) == true)
            return steamPath;

        return Path.GetDirectoryName(application.Path) ?? string.Empty;
    }

    private void RefreshApplicationsList()
    {
        _viewModel.PopulateApplicationsList(_viewModel.ApplicationsFolderPath);
    }

    private static void ShowErrorMessage(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}