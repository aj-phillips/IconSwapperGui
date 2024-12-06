using System.Net.Http;
using System.Windows;
using IconSwapperGui.Updater.Services;

namespace IconSwapperGui.Updater;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly UpdateService _updateService;

    public MainWindow()
    {
        InitializeComponent();

        var progressReporter = new Progress<string>(status => LabelStatus.Content = status);
        var downloadProgress = new Progress<double>(value => ProgressBarDownload.Value = value);
        var restartButtonVisibility = new Progress<Visibility>(visible => ButtonRestartInstall.Visibility = visible);
        var shouldCloseApplication = new Action<bool>(shouldClose =>
        {
            if (shouldClose) Environment.Exit(0);
        });

        var httpClient = new HttpClient();

        var fileDownloadService =
            new FileDownloadService(progressReporter, downloadProgress, restartButtonVisibility, httpClient);

        var githubReleaseService = new GithubReleaseService(httpClient);

        _updateService = new UpdateService(githubReleaseService, fileDownloadService, progressReporter,
            shouldCloseApplication);

        _updateService.CheckForUpdates();
    }

    private async void ButtonRestartInstall_Click(object sender, RoutedEventArgs e)
    {
        LabelStatus.Content = "Installing updates...";

        await _updateService.RunInstaller();
    }
}