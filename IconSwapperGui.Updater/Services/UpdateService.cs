using System.Diagnostics;
using System.IO;
using System.Windows;

namespace IconSwapperGui.Updater.Services;

public class UpdateService
{
    private const int MaxRetry = 5;

    private readonly string _currentAssemblyDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
    private readonly FileDownloadService _fileDownloadService;

    private readonly GithubReleaseService _githubReleaseService;
    private readonly IProgress<string> _progressReporter;
    private readonly Action<bool> _shouldCloseApplication;
    private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), "IconSwapperGui.exe");

    public UpdateService(GithubReleaseService githubReleaseService, FileDownloadService fileDownloadService,
        IProgress<string> progressReporter, Action<bool> shouldCloseApplication)
    {
        _githubReleaseService = githubReleaseService;
        _fileDownloadService = fileDownloadService;

        _progressReporter = progressReporter;
        _shouldCloseApplication = shouldCloseApplication;

        var args = Environment.GetCommandLineArgs();

        CurrentVersion = args.Length > 1
            ? args[1]
            : FileVersionInfo.GetVersionInfo(Path.Combine(_currentAssemblyDirectory, "IconSwapperGui.exe"))
                .ProductVersion;
    }

    private string CurrentVersion { get; }

    public async void CheckForUpdates()
    {
        _progressReporter.Report("Checking for updates...");

        var latestVersion = await _githubReleaseService.GetLatestReleaseVersion();

        var isNewVersionAvailable =
            string.Compare(latestVersion, CurrentVersion, StringComparison.OrdinalIgnoreCase) > 0;

        if (!isNewVersionAvailable) _shouldCloseApplication.Invoke(true);

        var updateResult = MessageBox.Show(
            $"There is an update available from {CurrentVersion} to {latestVersion}. Do you want to update now?",
            "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (updateResult != MessageBoxResult.Yes) _shouldCloseApplication.Invoke(true);

        _progressReporter.Report("Downloading updates...");

        var downloadUrl = "https://github.com/aj-phillips/IconSwapperGui/releases/download/v" + latestVersion +
                          "/IconSwapperGui.exe";

        await _fileDownloadService.DownloadFile(downloadUrl, _tempFilePath);
    }

    public async Task RunInstaller()
    {
        var applicationPath = Path.Combine(_currentAssemblyDirectory, "IconSwapperGui.exe");

        try
        {
            foreach (var process in Process.GetProcessesByName("IconSwapperGui")) process.Kill();

            var retryCount = 0;

            while (retryCount < MaxRetry)
                try
                {
                    await Task.Delay(2000);

                    if (File.Exists(applicationPath)) File.Delete(applicationPath);

                    File.Move(_tempFilePath, applicationPath, true);

                    break;
                }
                catch (IOException)
                {
                    retryCount++;
                    await Task.Delay(1000);
                }

            Process.Start(applicationPath);

            _shouldCloseApplication.Invoke(true);
        }
        catch (Exception exception)
        {
            _progressReporter.Report("Error: " + exception.Message);
        }
    }
}