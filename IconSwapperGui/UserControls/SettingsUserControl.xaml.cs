using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using IconSwapperGui.Services;
using IconSwapperGui.ViewModels;

namespace IconSwapperGui.UserControls;

public partial class SettingsUserControl : UserControl
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

    private void OpenWebsite(string url)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
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