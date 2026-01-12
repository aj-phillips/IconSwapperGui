using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using IconSwapperGui.Services;
using Serilog;

namespace IconSwapperGui.Windows;

public partial class OutOfSupportWarningWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly ILogger _logger = Log.ForContext<OutOfSupportWarningWindow>();

    public OutOfSupportWarningWindow()
    {
        InitializeComponent();
        _settingsService = new SettingsService();
    }

    private void GitHubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            _logger.Information("Opening GitHub releases page: {Uri}", e.Uri.AbsoluteUri);
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to open GitHub releases page");
            MessageBox.Show("Failed to open the link. Please manually navigate to:\n" + e.Uri.AbsoluteUri,
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (DontShowAgainCheckBox.IsChecked == true)
        {
            try
            {
                _logger.Information("User chose to hide out-of-support warning");
                _settingsService.SaveHideOutOfSupportWarning(true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save hide warning setting");
            }
        }

        Close();
    }
}
