using System.Diagnostics;
using System.Windows;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls;

public partial class SettingsUserControl
{
    public SettingsUserControl()
    {
        InitializeComponent();

        var settingsService = new SettingsService();

        var viewModel = new SettingsViewModel(settingsService);

        DataContext = viewModel;
    }

    private void GithubButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenWebsite("https://github.com/aj-phillips/IconSwapperGui");
    }

    private void BugReportButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenWebsite("https://github.com/aj-phillips/IconSwapperGui/issues/new/choose");
    }

    private static void OpenWebsite(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            Process.Start(psi);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"An error occurred: {exception.Message}", "Error Opening Website", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}