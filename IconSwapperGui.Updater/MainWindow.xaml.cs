using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows;

namespace IconSwapperGui.Updater;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string _downloadUrl;
    private readonly string _currentVersion;
    private readonly HttpClient _httpClient;

    private readonly string _tempFilePath =
        Path.Combine(Path.GetTempPath(), "IconSwapperGui.exe");

    private readonly string _currentAssemblyDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);

    private const int MaxRetry = 5;

    public MainWindow()
    {
        InitializeComponent();

        string[] args = Environment.GetCommandLineArgs();

        _currentVersion = args.Length > 1 ? args[1] : Assembly.GetExecutingAssembly().GetName().Version.ToString();

        _httpClient = new HttpClient();

        CheckForUpdates();
    }

    private async void CheckForUpdates()
    {
        LabelStatus.Content = "Checking for updates...";

        var latestVersion = await GetLatestReleaseVersion();

        var isNewVersionAvailable =
            string.Compare(latestVersion, _currentVersion, StringComparison.OrdinalIgnoreCase) > 0;

        if (!isNewVersionAvailable)
        {
            Environment.Exit(0);
        }

        var updateResult = MessageBox.Show(
            $"There is an update available from {_currentVersion} to {latestVersion}. Do you want to update now?",
            "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (updateResult != MessageBoxResult.Yes)
        {
            Environment.Exit(0);
        }

        LabelStatus.Content = "Downloading Updates...";

        //_downloadUrl = "https://github.com/aj-phillips/IconSwapperGui/releases/download/v" + latestVersion + "/IconSwapperGui.exe";
        _downloadUrl =
            "https://cdn.discordapp.com/attachments/318335789354188800/1247300598165143686/IconSwapperGui.exe?ex=665f86b4&is=665e3534&hm=a6d6599291c9113d2df5afc43826ba151182535813ae4c7cd6e94a4219e47d33&";

        await DownloadFile(_downloadUrl, _tempFilePath);
    }

    private async Task DownloadFile(string downloadUrl, string tempFilePath)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

            using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                if (!response.IsSuccessStatusCode)
                {
                    LabelStatus.Content = $"Error: {response.StatusCode} - {response.ReasonPhrase}";
                    return;
                }

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var canReportProgress = totalBytes != -1;

                await using (var output = File.Create(tempFilePath))
                {
                    await using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        var buffer = new byte[8192];
                        long totalBytesRead = 0;
                        int bytesRead;

                        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (canReportProgress)
                            {
                                ProgressBarDownload.Value = (double)totalBytesRead / totalBytes * 100;
                            }
                        }
                    }
                }

                // Check if there's a redirect and follow it if necessary
                if (response.Headers.Location != null)
                {
                    var redirectUrl = response.Headers.Location.ToString();
                    await DownloadFile(redirectUrl, tempFilePath); // Recursive call to handle redirect
                }
            }

            LabelStatus.Content = "Update is ready to install";
            ButtonRestartInstall.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            LabelStatus.Content = $"Error: {ex.Message}";
        }
    }

    private async Task<string> GetLatestReleaseVersion()
    {
        return "v10";
    }

    private async void ButtonRestartInstall_Click(object sender, RoutedEventArgs e)
    {
        LabelStatus.Content = "Installing updates...";

        await RunInstaller();
    }

    private async Task RunInstaller()
    {
        var applicationPath = Path.Combine(_currentAssemblyDirectory, "IconSwapperGui.exe");

        try
        {
            foreach (var process in Process.GetProcessesByName("IconSwapperGui"))
            {
                process.Kill();
            }

            var retryCount = 0;

            while (retryCount < MaxRetry)
            {
                try
                {
                    await Task.Delay(2000);
                    
                    if (File.Exists(applicationPath))
                    {
                        File.Delete(applicationPath);
                    }
                
                    File.Move(_tempFilePath, applicationPath, overwrite: true);
                
                    break;
                }
                catch (IOException)
                {
                    retryCount++;
                    await Task.Delay(1000);
                }
            }

            Process.Start(applicationPath);
            
            Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            if (exception.Message.Length > 25)
            {
                LabelStatus.FontSize = 13;
            }
            
            LabelStatus.Content = "Error: " + exception.Message;
        }
    }
}