using System.Windows;
using System.Windows.Input;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
        }

        private void CreditsTxt_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            LoveTxt.Visibility = Visibility.Visible;
        }

        private void CreditsTxt_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void CreditsTxt_OnMouseLeave(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }
    }
}