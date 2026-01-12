using IconSwapperGui.Models;

namespace IconSwapperGui.Services.Interfaces;

public interface ISettingsService
{
    Settings? GetSettings();
    T? GetSettingsFieldValue<T>(string fieldName);
    void SaveIconsLocation(string? iconsPath);
    void SaveIconsLocations(List<string>? iconsPaths);
    void SaveConverterIconsLocation(string? iconsPath);
    void SaveFoldersLocation(string? foldersPath);
    void SaveExportLocation(string? exportPath);
    void SaveApplicationsLocation(string? applicationsPath);
    void SaveApplicationsLocations(List<string>? applicationsPaths);
    void SaveFoldersLocations(List<string>? foldersPaths);
    void SaveEnableDarkMode(bool? enableDarkMode);
    void SaveEnableLaunchAtStartup(bool? enableLaunchAtStartup);
    void SaveEnableAutoUpdate(bool? enableAutoUpdate);
    void SaveEnableSeasonalEffects(bool? enableSeasonalEffects);
    void SaveHideOutOfSupportWarning(bool? hideOutOfSupportWarning);
    string? GetApplicationsLocation();
    List<string>? GetApplicationsLocations();
    string? GetIconsLocation();
    List<string>? GetIconsLocations();
    string? GetConverterIconsLocation();
    string? GetFoldersLocation();
    List<string>? GetFoldersLocations();
    bool? GetAutoUpdateValue();
    bool? GetSeasonalEffectsValue();
    bool? GetHideOutOfSupportWarningValue();
}