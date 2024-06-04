using System.IO;
using System.Net.Http;
using System.Windows;

namespace IconSwapperGui.Updater.Services;

public class FileDownloadService
{
    private readonly IProgress<string> _progressReporter;
    private readonly IProgress<double> _downloadProgress;
    private readonly IProgress<Visibility> _restartButtonVisibility;
    private HttpClient HttpClient { get; set; }

    public FileDownloadService(IProgress<string> progressReporter, IProgress<double> downloadProgress,
        IProgress<Visibility> restartButtonVisibility, HttpClient httpClient)
    {
        _progressReporter = progressReporter;
        _downloadProgress = downloadProgress;
        _restartButtonVisibility = restartButtonVisibility;
        HttpClient = httpClient;
    }

    public async Task DownloadFile(string downloadUrl, string tempFilePath)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

            using (var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                if (!response.IsSuccessStatusCode)
                {
                    _progressReporter.Report($"Error: {response.StatusCode} - {response.ReasonPhrase}");
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
                                _downloadProgress.Report((double)totalBytesRead / totalBytes * 100);
                            }
                        }
                    }
                }

                if (response.Headers.Location != null)
                {
                    var redirectUrl = response.Headers.Location.ToString();
                    await DownloadFile(redirectUrl, tempFilePath);
                }
            }

            _progressReporter.Report("Update is ready to install");
            _restartButtonVisibility.Report(Visibility.Visible);
        }
        catch (Exception ex)
        {
            _progressReporter.Report($"Error: {ex.Message}");
        }
    }
}