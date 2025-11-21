using System.Windows;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Windows;

public partial class ManageDirectoriesWindow : Window
{
    public ManageDirectoriesWindow(ManageDirectoriesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // forward LocationsChanged to owner if set via DataContext or view model
        viewModel.LocationsChanged += () => { /* handled by caller */ };
    }
}
