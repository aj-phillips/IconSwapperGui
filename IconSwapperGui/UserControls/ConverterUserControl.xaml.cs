using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls;

public partial class ConverterUserControl
{
    public ConverterUserControl()
    {
        InitializeComponent();

        var iconManagementService = new IconManagementService();
        var settingsService = new SettingsService();
        var dialogService = new DialogService();

        var viewModel = new ConverterViewModel(
            iconManagementService,
            settingsService,
            dialogService
        );

        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}