using System;
using System.IO;
using System.Windows.Controls;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;
using IconSwapperGui.Interfaces;

namespace IconSwapperGui.UserControls
{
    public partial class ConverterUserControl : UserControl
    {
        public ConverterUserControl()
        {
            InitializeComponent();

            var iconManagementService = new IconManagementService();
            var settingsService = new SettingsService();
            var dialogService = new DialogService();

            IFileSystemWatcherService FileSystemWatcherServiceFactory(string path,
                Action<object, FileSystemEventArgs> onChanged, Action<object, RenamedEventArgs> onRenamed) =>
                new FileSystemWatcherService(path, onChanged, onRenamed);

            var viewModel = new ConverterViewModel(
                iconManagementService,
                settingsService,
                dialogService,
                FileSystemWatcherServiceFactory
            );

            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }
    }
}