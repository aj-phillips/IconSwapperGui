using IconSwapperGui.Core.Config;
using IconSwapperGui.Core.Interfaces;
using CoreUpdateInfo = IconSwapperGui.Core.Models.UpdateInfo;
using Velopack;
using Velopack.Sources;

namespace IconSwapperGui.Infrastructure.Services;

public class VelopackUpdateService : IUpdateService
{
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _cachedVelopackUpdate;

    public VelopackUpdateService()
    {
        var githubSource = new GithubSource(
            $"https://github.com/{AppInfo.GitHubRepositoryOwner}/{AppInfo.GitHubRepositoryName}",
            null,
            false
        );
        
        _updateManager = new UpdateManager(githubSource);
    }

    public string GetCurrentVersion()
    {
        return AppInfo.GetApplicationVersion();
    }

    public async Task<CoreUpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            
            if (updateInfo == null)
            {
                _cachedVelopackUpdate = null;
                return null;
            }

            _cachedVelopackUpdate = updateInfo;

            return new CoreUpdateInfo
            {
                Version = updateInfo.TargetFullRelease.Version.ToString(),
                DownloadUrl = string.Empty,
                ReleaseNotes = string.Empty,
                PublishedAt = DateTime.Now
            };
        }
        catch
        {
            _cachedVelopackUpdate = null;
            return null;
        }
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(CoreUpdateInfo updateInfo)
    {
        try
        {
            if (_cachedVelopackUpdate == null)
            {
                var checkResult = await _updateManager.CheckForUpdatesAsync();
                if (checkResult == null)
                    return false;
                
                _cachedVelopackUpdate = checkResult;
            }

            await _updateManager.DownloadUpdatesAsync(_cachedVelopackUpdate);
            
            _updateManager.ApplyUpdatesAndRestart(_cachedVelopackUpdate.TargetFullRelease);
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}

