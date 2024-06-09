using System.IO;
using IconSwapperGui.ViewModels;
using ImageMagick;

namespace IconSwapperGui.Commands.IconConverter;

public class ConvertIconCommand : RelayCommand
{
    private readonly IconConverterViewModel _viewModel;

    public ConvertIconCommand(IconConverterViewModel viewModel, Action<object> execute,
        Func<object, bool>? canExecute = null) : base(execute, canExecute)
    {
        _viewModel = viewModel;
    }

    public override void Execute(object? parameter)
    {
        var directoryInfo = new DirectoryInfo(_viewModel.IconsFolderPath);

        foreach (var file in directoryInfo.GetFiles("*.png", SearchOption.TopDirectoryOnly))
        {
            var sourcePngPath = file.FullName;

            using (var collection = new MagickImageCollection())
            {
                collection.Add(sourcePngPath);

                var targetIconPath = sourcePngPath.Replace(".png", ".ico");

                collection.Write(targetIconPath, MagickFormat.Icon);
            }

            if (_viewModel.CanDeletePngImages)
            {
                file.Delete();
            }
        }

        _viewModel.DialogService.ShowInformation("Successfully converted PNG images to ICOs!", "Conversion Successful");
    }
}