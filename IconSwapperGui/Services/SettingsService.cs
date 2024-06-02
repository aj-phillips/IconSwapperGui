using System.IO;
using System.Text.Json;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;

namespace IconSwapperGui.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        CreateSettings();
    }

    public void CreateSettings()
    {
        if (File.Exists(_settingsFilePath)) return;

        var settingsObj = new Settings
        {
            IconLocation = "",
            ApplicationsLocation = ""
        };

        var settingsData = JsonSerializer.Serialize(settingsObj);

        File.WriteAllText(_settingsFilePath, settingsData);
    }

    public void SaveIconsLocation(string? iconsPath)
    {
        var settingsObj = GetSettings();

        if (settingsObj == null) return;

        settingsObj.IconLocation = iconsPath;

        var updatedSettingsData = JsonSerializer.Serialize(settingsObj);

        File.WriteAllText(_settingsFilePath, updatedSettingsData);
    }

    public void SaveApplicationsLocation(string? applicationsPath)
    {
        var settingsObj = GetSettings();

        if (settingsObj == null) return;

        settingsObj.ApplicationsLocation = applicationsPath;

        var updatedSettingsData = JsonSerializer.Serialize(settingsObj);

        File.WriteAllText(_settingsFilePath, updatedSettingsData);
    }

    public string? GetApplicationsLocation()
    {
        var settingsObj = GetSettings();

        return settingsObj?.ApplicationsLocation;
    }

    public string? GetIconsLocation()
    {
        var settingsObj = GetSettings();

        return settingsObj?.IconLocation;
    }

    public Settings? GetSettings()
    {
        if (!File.Exists(_settingsFilePath)) return null;

        var settingsData = File.ReadAllText(_settingsFilePath);
        var settingsObj = JsonSerializer.Deserialize<Settings>(settingsData);

        return settingsObj;
    }
}