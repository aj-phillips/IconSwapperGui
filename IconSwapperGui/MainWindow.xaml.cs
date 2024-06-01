using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IconSwapperGui.Models;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;
using Application = IconSwapperGui.Models.Application;

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
            var viewModel = new MainViewModel(applicationService, iconService, settingsService);

            DataContext = viewModel;
        }

        private void CreditsTxt_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            loveTxt.Visibility = Visibility.Visible;
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