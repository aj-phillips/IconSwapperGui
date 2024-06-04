using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _currentAssemblyDirectory = Path.GetDirectoryName(System.AppContext.BaseDirectory);
        private readonly string _currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        
        public MainWindow()
        {
            InitializeComponent();

            var settingsService = new SettingsService();
            var applicationService = new ApplicationService();
            var iconService = new IconService();
            var dialogService = new DialogService();
            var elevationService = new ElevationService();
            var viewModel = new MainViewModel(applicationService, iconService, settingsService, dialogService,
                elevationService);

            DataContext = viewModel;
            
            CheckForUpdates();
        }

        public void CheckForUpdates()
        {
            var updaterExePath = Path.Combine(_currentAssemblyDirectory, "IconSwapperGui.Updater.exe");

            Process.Start(updaterExePath, _currentVersion);
        }
    }
}