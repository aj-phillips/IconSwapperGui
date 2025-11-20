using System;
using System.IO;
using System.Threading.Tasks;
using IconSwapperGui.Helpers;
using IconSwapperGui.ViewModels;
using IconSwapperGui.Services.Interfaces;

namespace IconSwapperGui.Commands.Swapper;

public class SwapCommand : RelayCommand
{
    private readonly LnkIconSwapper _lnkIconSwapper;
    private readonly UrlIconSwapper _urlIconSwapper;
    private readonly SwapperViewModel _viewModel;
    private readonly IIconHistoryService? _historyService;

    public SwapCommand(SwapperViewModel viewModel, Action<object> execute, Func<object, bool>? canExecute = null,
        IIconHistoryService? historyService = null)
        : base(execute, canExecute)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _lnkIconSwapper = new LnkIconSwapper(viewModel.DialogService, viewModel.ElevationService);
        _urlIconSwapper = new UrlIconSwapper();
        _historyService = historyService;
    }

    public override async void Execute(object? parameter)
    {
        try
        {
            if (_viewModel.SelectedApplication == null || _viewModel.SelectedIcon == null)
            {
                _viewModel.DialogService.ShowWarning("Please select an application and an icon to swap.",
                    "No Application or Icon Selected");
                return;
            }

            try
            {
                if (_historyService != null)
                {
                    var filePath = _viewModel.SelectedApplication.Path;
                    string? currentIconPath = null;

                    try
                    {
                        currentIconPath = await _viewModel.GetCurrentIconPathAsync(filePath);
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(currentIconPath))
                    {
                        try
                        {
                            if (File.Exists(currentIconPath))
                            {
                                var ext = Path.GetExtension(currentIconPath)?.ToLowerInvariant();
                                if (ext == ".exe" || ext == ".dll")
                                {
                                    try
                                    {
                                        using var icon = System.Drawing.Icon.ExtractAssociatedIcon(currentIconPath);
                                        if (icon != null)
                                        {
                                            var tempIconPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ico");
                                            using (var fs = new FileStream(tempIconPath, FileMode.Create, FileAccess.Write))
                                            {
                                                icon.Save(fs);
                                            }

                                            currentIconPath = tempIconPath;
                                        }
                                    }
                                    catch
                                    {
                                        // ignore
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // ignore
                        }

                        var existing = await _historyService.GetHistoryAsync(filePath);
                        if (existing == null || existing.Versions.Count == 0)
                        {
                            try
                            {
                                await _historyService.RecordIconChangeAsync(filePath, currentIconPath);
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }

            var extension = Path.GetExtension(_viewModel.SelectedApplication.Path)?.ToLowerInvariant();

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

            try
            {
                if (_historyService != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _historyService.RecordIconChangeAsync(_viewModel.SelectedApplication.Path, _viewModel.SelectedIcon.Path);
                        }
                        catch { }
                    });
                }
            }
            catch
            {
                // ignore
            }

            await _viewModel.ShowSuccessTick();
            _viewModel.ResetGui();
        }
        catch (Exception ex)
        {
            _viewModel.DialogService.ShowError(
                $"An error occurred while swapping the icon for {_viewModel.SelectedApplication?.Name}: {ex.Message}",
                "Error Swapping Icon");
        }
    }
}