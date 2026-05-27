using System.Windows;
using IconSwapperGui.UI.ViewModels;
using IconSwapperGui.UI.Windows;

namespace IconSwapperGui.UI.Services;

public interface IWindowService
{
    bool? ShowIconVersionManager(IconVersionManagerViewModel viewModel);
    bool? ShowRenameIcon(RenameIconViewModel viewModel);
}

public sealed class WindowService : IWindowService
{
    public bool? ShowIconVersionManager(IconVersionManagerViewModel viewModel)
    {
        var window = new IconVersionManagerWindow
        {
            Owner = Application.Current?.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += () => window.Close();

        return window.ShowDialog();
    }

    public bool? ShowRenameIcon(RenameIconViewModel viewModel)
    {
        var window = new RenameIconWindow
        {
            Owner = Application.Current?.MainWindow,
            DataContext = viewModel
        };

        viewModel.RequestClose += result =>
        {
            window.DialogResult = result;
            window.Close();
        };

        return window.ShowDialog();
    }
}
