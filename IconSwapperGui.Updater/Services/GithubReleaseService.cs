using System.Net.Http;
using System.Text.Json;
using IconSwapperGui.Updater.Models;

namespace IconSwapperGui.Updater.Services;

public class GithubReleaseService
{
    private const string RawFileUrl = "https://raw.githubusercontent.com/aj-phillips/IconSwapperGui/main/version.json";

    public GithubReleaseService(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    private HttpClient HttpClient { get; }

    public async Task<string> GetLatestReleaseVersion()
    {
        try
        {
            var json = await HttpClient.GetStringAsync(RawFileUrl);
            var data = JsonSerializer.Deserialize<VersionData>(json);
            return data?.LatestVersion ?? "1.0.0";
        }
        catch (HttpRequestException e)
        {
            return "1.0.0";
        }
        catch (JsonException e)
        {
            return "1.0.0";
        }
    }
}