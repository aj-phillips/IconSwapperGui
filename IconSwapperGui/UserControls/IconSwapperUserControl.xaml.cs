using System.Windows.Controls;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls;

public partial class IconSwapperUserControl : UserControl
{
    public IconSwapperUserControl()
    {
        InitializeComponent();
        
        var settingsService = new SettingsService();
        var applicationService = new ApplicationService();
        var iconService = new IconService();
        var dialogService = new DialogService();
        var elevationService = new ElevationService();
        var viewModel = new IconSwapperViewModel(applicationService, iconService, settingsService, dialogService,
            elevationService);

        DataContext = viewModel;
    }
}