using IconSwapperGui.Models;

namespace IconSwapperGui.Services.Interfaces;

public interface ISettingsService
{
    Settings? GetSettings();
    T? GetSettingsFieldValue<T>(string fieldName);
    void SaveIconsLocation(string? iconsPath);
    void SaveConverterIconsLocation(string? iconsPath);
    void SaveExportLocation(string? exportPath);
    void SaveApplicationsLocation(string? applicationsPath);
    void SaveEnableDarkMode(bool? enableDarkMode);
    void SaveEnableLaunchAtStartup(bool? enableLaunchAtStartup);
    void SaveEnableAutoUpdate(bool? enableAutoUpdate);
    void SaveEnableSeasonalEffects(bool? enableSeasonalEffects);
    string? GetApplicationsLocation();
    string? GetIconsLocation();
    string? GetConverterIconsLocation();
    bool? GetAutoUpdateValue();
    bool? GetSeasonalEffectsValue();
}