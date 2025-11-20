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
        var iconManagementService = new IconManagementService();
        var dialogService = new DialogService();
        var elevationService = new ElevationService();
        var iconHistoryService = new IconHistoryService(iconManagementService);
        var viewModel = new SwapperViewModel(applicationService, iconManagementService, settingsService, dialogService,
            elevationService, iconHistoryService);

        DataContext = viewModel;
    }
}