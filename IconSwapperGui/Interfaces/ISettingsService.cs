using IconSwapperGui.Models;

namespace IconSwapperGui.Interfaces;

public interface ISettingsService
{
    Settings? GetSettings();
    T? GetSettingsFieldValue<T>(string fieldName);
    void SaveIconsLocation(string? iconsPath);
    void SaveConverterIconsLocation(string? iconsPath);
    void SaveApplicationsLocation(string? applicationsPath);
    void SaveEnableDarkMode(bool? enableDarkMode);
    void SaveEnableLaunchAtStartup(bool? enableLaunchAtStartup);
    void SaveEnableAutoUpdate(bool? enableAutoUpdate);

    string? GetApplicationsLocation();
    string? GetIconsLocation();
    string? GetConverterIconsLocation();
    bool? GetAutoUpdateValue();
}