using IconSwapperGui.Helpers;
using IconSwapperGui.ViewModels;
using Serilog;
using System.IO;

namespace IconSwapperGui.Commands.Converter;

public class ConvertIconCommand : RelayCommand
{
    private static readonly int[] IconSizes = new[] { 16, 32, 48, 64, 128, 256 };
    private static readonly string[] SupportedExtensions = new[] { "*.png", "*.jpg", "*.jpeg" };
    private readonly ConverterViewModel _viewModel;
    private readonly ILogger _logger = Log.ForContext<ConvertIconCommand>();

    public ConvertIconCommand(ConverterViewModel viewModel, Action<object> execute,
        Func<object, bool>? canExecute = null) : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        if (_viewModel.IconsFolderPath != null)
        {
            var directoryInfo = new DirectoryInfo(_viewModel.IconsFolderPath);

            foreach (var extension in SupportedExtensions)
            foreach (var file in directoryInfo.GetFiles(extension, SearchOption.TopDirectoryOnly))
                ProcessFile(file);
        }

        NotifyCompletion();
    }

    private void ProcessFile(FileInfo file)
    {
        try
        {
            var sourceImagePath = file.FullName;
            var targetIconPath = GetTargetIconPath(sourceImagePath);

            RemoveExistingFile(targetIconPath);
            ConvertImage(sourceImagePath, targetIconPath);

            if (_viewModel.CanDeleteImagesAfterConversion) file.Delete();
        }
        catch (Exception ex)
        {
            HandleException(file, ex);
        }
    }

    private string GetTargetIconPath(string sourceImagePath)
    {
        return Path.ChangeExtension(sourceImagePath, ".ico");
    }

    private static void RemoveExistingFile(string targetIconPath)
    {
        if (File.Exists(targetIconPath)) File.Delete(targetIconPath);
    }

    private void ConvertImage(string sourceImagePath, string targetIconPath)
    {
        IconCreator.CreateMultiSizeIcoFromImage(sourceImagePath, targetIconPath, IconSizes);
    }

    private void HandleException(FileInfo file, Exception ex)
    {
        _logger.Error(ex, "Failed to convert image to icon: {FileName}", file.Name);
        _viewModel.DialogService.ShowError("Conversion Error",
            $"Failed to convert image '{file.Name}' to icon.\n\nError: {ex.Message}");
    }

    private void NotifyCompletion()
    {
        _viewModel.DialogService.ShowInformation("Successfully converted images to ICOs!", "Conversion Successful");
        _viewModel.RefreshGui();
    }
}