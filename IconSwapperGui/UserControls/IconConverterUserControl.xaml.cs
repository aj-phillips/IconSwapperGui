using System.Windows.Controls;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls;

public partial class IconConverterUserControl : UserControl
{
    public IconConverterUserControl()
    {
        InitializeComponent();

        var iconService = new IconService();
        var settingsService = new SettingsService();
        var dialogService = new DialogService();
        var viewModel = new IconConverterViewModel(iconService, settingsService, dialogService);

        DataContext = viewModel;
    }
}