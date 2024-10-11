using System;
using System.IO;
using IconSwapperGui.ViewModels;
using ImageMagick;

namespace IconSwapperGui.Commands.Converter
{
    public class ConvertIconCommand : RelayCommand
    {
        private readonly ConverterViewModel _viewModel;

        private static readonly uint[] IconSizes = [94];
        private static readonly string[] SupportedExtensions = ["*.png", "*.jpg", "*.jpeg"];

        public ConvertIconCommand(ConverterViewModel viewModel, Action<object> execute,
            Func<object, bool>? canExecute = null) : base(execute, canExecute)
        {
            _viewModel = viewModel;
        }

        public override void Execute(object? parameter)
        {
            var directoryInfo = new DirectoryInfo(_viewModel.IconsFolderPath);

            foreach (var extension in SupportedExtensions)
            {
                foreach (var file in directoryInfo.GetFiles(extension, SearchOption.TopDirectoryOnly))
                {
                    ProcessFile(file);
                }
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

                if (_viewModel.CanDeleteImagesAfterConversion)
                {
                    file.Delete();
                }
            }
            catch (Exception ex) when (ex is MagickException or IOException)
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
            if (File.Exists(targetIconPath))
            {
                File.Delete(targetIconPath);
            }
        }

        private void ConvertImage(string sourceImagePath, string targetIconPath)
        {
            using var collection = new MagickImageCollection();

            foreach (var size in IconSizes)
            {
                var image = new MagickImage(sourceImagePath);
                image.Resize(size, size);
                collection.Add(image);
            }

            collection.Write(targetIconPath, MagickFormat.Icon);
        }

        private void HandleException(FileInfo file, Exception ex)
        {
            var errorType = ex is MagickException
                ? "Failed to convert image to icon"
                : "An error occurred while processing the file";
            throw new InvalidOperationException($"{errorType}: {file.Name}\n{ex.Message}", ex);
        }

        private void NotifyCompletion()
        {
            _viewModel.DialogService.ShowInformation("Successfully converted images to ICOs!", "Conversion Successful");
            _viewModel.RefreshGui();
        }
    }
}