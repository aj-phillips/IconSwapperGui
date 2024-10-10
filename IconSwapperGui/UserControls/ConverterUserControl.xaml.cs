using System.Windows.Controls;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls;

public partial class ConverterUserControl : UserControl
{
    public ConverterUserControl()
    {
        InitializeComponent();

        var iconService = new IconService();
        var settingsService = new SettingsService();
        var dialogService = new DialogService();
        var viewModel = new ConverterViewModel(iconService, settingsService, dialogService);

        DataContext = viewModel;
    }
}