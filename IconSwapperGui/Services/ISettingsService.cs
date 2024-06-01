using IconSwapperGui.Models;

namespace IconSwapperGui.Services;

public interface ISettingsService
{
    public void CreateSettings();
    public void SaveIconsLocation(string? iconsPath);
    public void SaveApplicationsLocation(string? applicationsPath);
    public string? GetApplicationsLocation();
    public string? GetIconsLocation();
    public Settings? GetSettings();
}