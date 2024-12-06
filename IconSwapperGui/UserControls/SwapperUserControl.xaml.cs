using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls;

public partial class SwapperUserControl
{
    public SwapperUserControl()
    {
        InitializeComponent();

        var settingsService = new SettingsService();
        var applicationService = new ApplicationService();
        var iconService = new IconManagementService();
        var dialogService = new DialogService();
        var elevationService = new ElevationService();
        var viewModel = new SwapperViewModel(applicationService, iconService, settingsService, dialogService,
            elevationService);

        DataContext = viewModel;
    }
}