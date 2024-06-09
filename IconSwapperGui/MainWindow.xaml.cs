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
            
            if (!Debugger.IsAttached)
            {
                CheckForUpdates();
            }
        }

        public void CheckForUpdates()
        {
            var updaterExePath = Path.Combine(_currentAssemblyDirectory, "IconSwapperGui.Updater.exe");

            Process.Start(updaterExePath, _currentVersion);
        }
    }
}