using IconSwapperGui.Models;

namespace IconSwapperGui.Interfaces;

public interface ISettingsService
{
    public void CreateSettings();
    public void SaveIconsLocation(string? iconsPath);
    public void SaveConverterIconsLocation(string? iconsPath);
    public void SaveApplicationsLocation(string? applicationsPath);
    public string? GetApplicationsLocation();
    public string? GetIconsLocation();
    public string? GetConverterIconsLocation();
    public Settings? GetSettings();
}