using System.IO;
using System.Windows.Controls;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls;

public partial class ConverterUserControl : UserControl
{
    public ConverterUserControl()
    {
        InitializeComponent();

        var iconManagementService = new IconManagementService();
        var settingsService = new SettingsService();
        var dialogService = new DialogService();

        IFileSystemWatcherService FileSystemWatcherServiceFactory(string path,
            Action<object, FileSystemEventArgs> onChanged, Action<object, RenamedEventArgs> onRenamed)
        {
            return new FileSystemWatcherService(path, onChanged, onRenamed);
        }

        var viewModel = new ConverterViewModel(
            iconManagementService,
            settingsService,
            dialogService,
            FileSystemWatcherServiceFactory
        );

        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}