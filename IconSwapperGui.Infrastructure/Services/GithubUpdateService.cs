using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using IconSwapperGui.Core.Config;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Infrastructure.Services;

public class GitHubUpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _repoName;
    private readonly string _repoOwner;

    public GitHubUpdateService(string repoOwner, string repoName)
    {
        _repoOwner = repoOwner;
        _repoName = repoName;

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", AppInfo.ApplicationName);
    }

    public GitHubUpdateService()
        : this(AppInfo.GitHubRepositoryOwner, AppInfo.GitHubRepositoryName)
    {
    }

    public string GetCurrentVersion()
    {
        var appVersionRaw = AppInfo.GetApplicationVersion();
        var version = new Version(appVersionRaw);

        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
    }

    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
            var response = await _httpClient.GetStringAsync(url);

            var jsonDoc = JsonDocument.Parse(response);
            var root = jsonDoc.RootElement;

            var latestVersion = root.GetProperty("tag_name").GetString()?.TrimStart('v') ?? "0.0.0";
            var currentVersion = GetCurrentVersion();

            if (IsNewerVersion(latestVersion, currentVersion))
                return new UpdateInfo
                {
                    Version = latestVersion,
                    DownloadUrl = root.GetProperty("html_url").GetString() ?? string.Empty,
                    ReleaseNotes = root.GetProperty("body").GetString() ?? string.Empty,
                    PublishedAt = root.GetProperty("published_at").GetDateTime()
                };

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = updateInfo.DownloadUrl,
                UseShellExecute = true
            });

            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool IsNewerVersion(string latestVersion, string currentVersion)
    {
        try
        {
            var latest = new Version(latestVersion);
            var current = new Version(currentVersion);
            return latest > current;
        }
        catch
        {
            return false;
        }
    }
}