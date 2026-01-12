using System.Windows;
using IconSwapperGui.Core.Config;
using IconSwapperGui.UI.ViewModels;

namespace IconSwapperGui.UI.Windows;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        Title = AppInfo.ApplicationName;
        DataContext = vm;
    }
}