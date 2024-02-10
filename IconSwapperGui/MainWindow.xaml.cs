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

namespace IconSwapperGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<Icon> Icons { get; set; } = new List<Icon>
        {
            new Icon { Name = "Icon 1", Path = "C:\\path\\to\\icon1.ico" },
            new Icon { Name = "Icon 2", Path = "C:\\path\\to\\icon2.ico" },
            new Icon { Name = "Icon 3", Path = "C:\\path\\to\\icon3.ico" }
        };

        public MainWindow()
        {
            InitializeComponent();
            iconsControl.ItemsSource = Icons;
        }
    }
}