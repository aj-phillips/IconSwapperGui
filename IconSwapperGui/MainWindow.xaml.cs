using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

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
            
            this.Title = $"Icon Swapper - v{_currentVersion}";
            
            if (!Debugger.IsAttached)
            {
                CheckForUpdates();
            }
        }

        public void CheckForUpdates()
        {
            var updaterExePath = Path.Combine(_currentAssemblyDirectory, "IconSwapperGui.Updater.exe");

            if (File.Exists(updaterExePath))
            {
                Process.Start(updaterExePath, _currentVersion);
            }
            else
            {
                MessageBox.Show("Updater could not be found. The application will start without updating.", "Updater Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}