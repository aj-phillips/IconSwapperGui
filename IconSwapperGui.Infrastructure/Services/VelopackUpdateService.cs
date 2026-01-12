using IconSwapperGui.Core.Config;
using IconSwapperGui.Core.Interfaces;
using CoreUpdateInfo = IconSwapperGui.Core.Models.UpdateInfo;
using Velopack;
using Velopack.Sources;
using Velopack.Locators;
using Velopack.Logging;

namespace IconSwapperGui.Infrastructure.Services;

public class VelopackUpdateService : IUpdateService
{
    private readonly UpdateManager _updateManager;
    private readonly ILoggingService _loggingService;
    private UpdateInfo? _cachedVelopackUpdate;

    public VelopackUpdateService(ILoggingService loggingService)
    {
        _loggingService = loggingService;

        try
        {
            var githubSource = new GithubSource(
                $"https://github.com/{AppInfo.GitHubRepositoryOwner}/{AppInfo.GitHubRepositoryName}",
                null,
                false
            );

            var velopackLogger = new LoggingServiceAdapter(loggingService);
            var locator = VelopackLocator.CreateDefaultForPlatform(velopackLogger);

            _updateManager = new UpdateManager(githubSource, null, locator);
            _loggingService.LogInfo(
                $"VelopackUpdateService initialized with GitHub source: {AppInfo.GitHubRepositoryOwner}/{AppInfo.GitHubRepositoryName}");
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to initialize VelopackUpdateService", ex);
            throw;
        }
    }

    public string GetCurrentVersion()
    {
        var version = AppInfo.GetApplicationVersion();
        _loggingService.LogInfo($"Current application version: {version}");
        return version;
    }

    public async Task<CoreUpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            _loggingService.LogInfo("Checking for updates...");
            var updateInfo = await _updateManager.CheckForUpdatesAsync();

            if (updateInfo == null)
            {
                _loggingService.LogInfo("No updates available");
                _cachedVelopackUpdate = null;
                return null;
            }

            _cachedVelopackUpdate = updateInfo;
            _loggingService.LogInfo($"Update available: {updateInfo.TargetFullRelease.Version}");

            return new CoreUpdateInfo
            {
                Version = updateInfo.TargetFullRelease.Version.ToString(),
                DownloadUrl = string.Empty,
                ReleaseNotes = string.Empty,
                PublishedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to check for updates", ex);
            _cachedVelopackUpdate = null;
            return null;
        }
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(CoreUpdateInfo updateInfo)
    {
        try
        {
            _loggingService.LogInfo($"Starting update process for version: {updateInfo.Version}");

            if (_cachedVelopackUpdate == null)
            {
                _loggingService.LogInfo("No cached update info, checking for updates...");
                var checkResult = await _updateManager.CheckForUpdatesAsync();
                if (checkResult == null)
                {
                    _loggingService.LogWarning("No update found during check");
                    return false;
                }

                _cachedVelopackUpdate = checkResult;
                _loggingService.LogInfo($"Update found: {_cachedVelopackUpdate.TargetFullRelease.Version}");
            }

            _loggingService.LogInfo("Downloading update...");
            await _updateManager.DownloadUpdatesAsync(_cachedVelopackUpdate);
            _loggingService.LogInfo("Update downloaded successfully");

            _loggingService.LogInfo("Applying update and restarting...");
            _updateManager.ApplyUpdatesAndRestart(_cachedVelopackUpdate.TargetFullRelease);

            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogError("Failed to download and install update", ex);
            return false;
        }
    }
}

/// <summary>
/// Adapter to bridge Velopack's IVelopackLogger to our ILoggingService
/// </summary>
internal class LoggingServiceAdapter : IVelopackLogger
{
    private readonly ILoggingService _loggingService;

    public LoggingServiceAdapter(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public void Log(VelopackLogLevel logLevel, string? message, Exception? exception)
    {
        var logMessage = $"[VELOPACK] {message}";

        switch (logLevel)
        {
            case VelopackLogLevel.Debug:
                _loggingService.LogDebug(logMessage);
                break;
            case VelopackLogLevel.Information:
                _loggingService.LogInfo(logMessage);
                break;
            case VelopackLogLevel.Warning:
                _loggingService.LogWarning(logMessage);
                break;
            case VelopackLogLevel.Error:
                _loggingService.LogError(logMessage, exception);
                break;
        }
    }
}