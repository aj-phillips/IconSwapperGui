using System.IO;
using System.Text.Json;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using Newtonsoft.Json.Linq;

namespace IconSwapperGui.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    public SettingsService()
    {
        _settingsFilePath = GetSettingsFilePath();
        EnsureSettingsFileExists();
        UpdateSettingsWithDefaults();
    }

    public Settings? GetSettings()
    {
        if (!File.Exists(_settingsFilePath)) return null;
        var settingsData = File.ReadAllText(_settingsFilePath);
        return DeserializeSettings(settingsData);
    }

    public T? GetSettingsFieldValue<T>(string fieldName)
    {
        var settings = ReadJsonFile();
        return settings.TryGetValue(fieldName, out var fieldValue) ? fieldValue.ToObject<T>() : default;
    }

    public void SaveIconsLocation(string? iconsPath)
    {
        UpdateSettingsProperty((settings, value) => settings.IconLocation = value, iconsPath);
    }

    public void SaveConverterIconsLocation(string? iconsPath)
    {
        UpdateSettingsProperty((settings, value) => settings.ConverterIconLocation = value, iconsPath);
    }

    public void SaveApplicationsLocation(string? applicationsPath)
    {
        UpdateSettingsProperty((settings, value) => settings.ApplicationsLocation = value, applicationsPath);
    }

    public void SaveEnableDarkMode(bool? enableDarkMode)
    {
        UpdateSettingsProperty((settings, value) => settings.EnableDarkMode = value, enableDarkMode);
    }

    public void SaveEnableLaunchAtStartup(bool? enableLaunchAtStartup)
    {
        UpdateSettingsProperty((settings, value) => settings.EnableLaunchAtStartup = value, enableLaunchAtStartup);
    }

    public void SaveEnableAutoUpdate(bool? enableAutoUpdate)
    {
        UpdateSettingsProperty((settings, value) => settings.EnableAutoUpdate = value, enableAutoUpdate);
    }

    public string? GetApplicationsLocation()
    {
        return GetSettingsFieldValue<string>("ApplicationsLocation");
    }

    public string? GetIconsLocation()
    {
        return GetSettingsFieldValue<string>("IconLocation");
    }

    public string? GetConverterIconsLocation()
    {
        return GetSettingsFieldValue<string>("ConverterIconLocation");
    }

    public bool? GetAutoUpdateValue()
    {
        return GetSettingsFieldValue<bool>("EnableAutoUpdate");
    }

    private static string GetSettingsFilePath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    }

    private void EnsureSettingsFileExists()
    {
        if (File.Exists(_settingsFilePath)) return;
        SaveSettings(GetDefaultSettings());
    }

    private Settings GetDefaultSettings()
    {
        return new Settings
        {
            IconLocation = "",
            ConverterIconLocation = "",
            ApplicationsLocation = "",
            EnableDarkMode = false,
            EnableLaunchAtStartup = false,
            EnableAutoUpdate = true
        };
    }

    private JObject ReadJsonFile()
    {
        var jsonData = File.ReadAllText(_settingsFilePath);
        return JObject.Parse(jsonData);
    }

    private void UpdateSettingsProperty<T>(Action<Settings, T> updateAction, T value)
    {
        var settings = GetSettings() ?? new Settings();
        updateAction(settings, value);
        SaveSettings(settings);
    }

    private void UpdateSettingsWithDefaults()
    {
        var settings = GetSettings() ?? new Settings();

        settings.IconLocation ??= "";
        settings.ConverterIconLocation ??= "";
        settings.ApplicationsLocation ??= "";
        settings.EnableDarkMode ??= false;
        settings.EnableLaunchAtStartup ??= false;
        settings.EnableAutoUpdate ??= true;

        SaveSettings(settings);
    }

    public void SaveSettings(Settings settings)
    {
        var settingsData = SerializeSettings(settings);
        File.WriteAllText(_settingsFilePath, settingsData);
    }

    private string SerializeSettings(Settings settings)
    {
        return JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
    }

    private Settings? DeserializeSettings(string settingsData)
    {
        return JsonSerializer.Deserialize<Settings>(settingsData);
    }
}