using System.Windows;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.Windows;

public partial class IconVersionManagerWindow : Window
{
    public IconVersionManagerWindow(string filePath)
    {
        InitializeComponent();
        
        var iconManagementService = new IconManagementService();
        var iconHistoryService = new IconHistoryService(iconManagementService);
        var dialogService = new DialogService();
        
        var viewModel = new IconVersionManagerViewModel(iconHistoryService, dialogService, filePath);
        
        viewModel.RequestClose += Close;
        viewModel.VersionReverted += _ => DialogResult = true;

        DataContext = viewModel;
    }
}