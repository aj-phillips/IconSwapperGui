using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using IconSwapperGui.Models;
using IconSwapperGui.Services.Interfaces;
using Newtonsoft.Json.Linq;
using Serilog;
using Application = System.Windows.Application;

namespace IconSwapperGui.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly ILogger _logger = Log.ForContext<SettingsService>();

    public SettingsService()
    {
        _settingsFilePath = GetSettingsFilePath();
        _logger.Information("SettingsService initialized with settings file path: {SettingsFilePath}",
            _settingsFilePath);

        EnsureSettingsFileExists();
        UpdateSettingsWithDefaults();
    }

    public Settings? GetSettings()
    {
        _logger.Information("Getting all settings from file");

        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.Warning("Settings file does not exist at: {SettingsFilePath}", _settingsFilePath);
                return null;
            }

            var settingsData = File.ReadAllText(_settingsFilePath);
            var settings = DeserializeSettings(settingsData);

            _logger.Information("Successfully loaded settings from file");
            return settings;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error reading settings from file: {SettingsFilePath}", _settingsFilePath);
            throw;
        }
    }

    public T? GetSettingsFieldValue<T>(string fieldName)
    {
        _logger.Information("Getting settings field value for: {FieldName}", fieldName);

        try
        {
            var settings = ReadJsonFile();

            if (settings.TryGetValue(fieldName, out var fieldValue))
            {
                var value = fieldValue.ToObject<T>();
                _logger.Information("Successfully retrieved field {FieldName} with value: {Value}", fieldName, value);
                return value;
            }

            _logger.Warning("Field {FieldName} not found in settings, returning default value", fieldName);
            return default;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting settings field value for: {FieldName}", fieldName);
            throw;
        }
    }

    public void SaveIconsLocation(string? iconsPath)
    {
        _logger.Information("Saving icons location: {IconsPath}", iconsPath ?? "null");
        UpdateSettingsProperty((settings, value) => settings.IconLocation = value, iconsPath);
    }

    public void SaveExportLocation(string? exportPath)
    {
        _logger.Information("Saving export location: {ExportPath}", exportPath ?? "null");
        UpdateSettingsProperty((settings, value) => settings.ExportLocation = value, exportPath);
    }

    public void SaveConverterIconsLocation(string? iconsPath)
    {
        _logger.Information("Saving converter icons location: {IconsPath}", iconsPath ?? "null");
        UpdateSettingsProperty((settings, value) => settings.ConverterIconLocation = value, iconsPath);
    }

    public void SaveApplicationsLocation(string? applicationsPath)
    {
        _logger.Information("Saving applications location: {ApplicationsPath}", applicationsPath ?? "null");
        UpdateSettingsProperty((settings, value) => settings.ApplicationsLocation = value, applicationsPath);
    }

    public void SaveEnableDarkMode(bool? enableDarkMode)
    {
        _logger.Information("Saving enable dark mode: {EnableDarkMode}", enableDarkMode);
        UpdateSettingsProperty((settings, value) => settings.EnableDarkMode = value, enableDarkMode);
    }

    public void SaveEnableLaunchAtStartup(bool? enableLaunchAtStartup)
    {
        _logger.Information("Saving enable launch at startup: {EnableLaunchAtStartup}", enableLaunchAtStartup);
        UpdateSettingsProperty((settings, value) => settings.EnableLaunchAtStartup = value, enableLaunchAtStartup);
    }

    public void SaveEnableAutoUpdate(bool? enableAutoUpdate)
    {
        _logger.Information("Saving enable auto update: {EnableAutoUpdate}", enableAutoUpdate);
        UpdateSettingsProperty((settings, value) => settings.EnableAutoUpdate = value, enableAutoUpdate);
    }

    public void SaveEnableSeasonalEffects(bool? enableSeasonalEffects)
    {
        _logger.Information("Saving enable seasonal effects: {EnableSeasonalEffects}", enableSeasonalEffects);
        UpdateSettingsProperty((settings, value) => settings.EnableSeasonalEffects = value, enableSeasonalEffects);

        var restartMessageBoxResult = MessageBox.Show(
            "Please restart the application to apply the changes.\n\nWould you like to do this now?", "Restart",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (restartMessageBoxResult != MessageBoxResult.Yes)
        {
            _logger.Information("User declined to restart application");
            return;
        }

        try
        {
            var executablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IconSwapperGui.exe");
            _logger.Information("Restarting application from: {ExecutablePath}", executablePath);

            Process.Start(executablePath);
            _logger.Information("Application restart initiated, shutting down current instance");

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error restarting application");
            throw;
        }
    }

    public string? GetApplicationsLocation()
    {
        var location = GetSettingsFieldValue<string>("ApplicationsLocation");
        _logger.Information("Retrieved applications location: {Location}", location ?? "null");
        return location;
    }

    public string? GetIconsLocation()
    {
        var location = GetSettingsFieldValue<string>("IconLocation");
        _logger.Information("Retrieved icons location: {Location}", location ?? "null");
        return location;
    }

    public string? GetConverterIconsLocation()
    {
        var location = GetSettingsFieldValue<string>("ConverterIconLocation");
        _logger.Information("Retrieved converter icons location: {Location}", location ?? "null");
        return location;
    }

    public string? GetExportLocation()
    {
        var location = GetSettingsFieldValue<string>("ExportLocation");
        _logger.Information("Retrieved export location: {Location}", location ?? "null");
        return location;
    }

    public bool? GetAutoUpdateValue()
    {
        var value = GetSettingsFieldValue<bool>("EnableAutoUpdate");
        _logger.Information("Retrieved auto update value: {Value}", value);
        return value;
    }

    public bool? GetSeasonalEffectsValue()
    {
        var value = GetSettingsFieldValue<bool>("EnableSeasonalEffects");
        _logger.Information("Retrieved seasonal effects value: {Value}", value);
        return value;
    }

    private static string GetSettingsFilePath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    }

    private void EnsureSettingsFileExists()
    {
        if (File.Exists(_settingsFilePath))
        {
            _logger.Information("Settings file already exists at: {SettingsFilePath}", _settingsFilePath);
            return;
        }

        _logger.Warning("Settings file does not exist, creating with default settings at: {SettingsFilePath}",
            _settingsFilePath);
        SaveSettings(GetDefaultSettings());
    }

    private Settings GetDefaultSettings()
    {
        _logger.Information("Creating default settings");

        return new Settings
        {
            ExportLocation = "",
            IconLocation = "",
            ConverterIconLocation = "",
            ApplicationsLocation = "",
            EnableDarkMode = false,
            EnableLaunchAtStartup = false,
            EnableAutoUpdate = true,
            EnableSeasonalEffects = true
        };
    }

    private JObject ReadJsonFile()
    {
        try
        {
            _logger.Information("Reading JSON from settings file");
            var jsonData = File.ReadAllText(_settingsFilePath);
            var parsed = JObject.Parse(jsonData);
            _logger.Information("Successfully parsed settings JSON");
            return parsed;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error reading or parsing JSON from settings file: {SettingsFilePath}",
                _settingsFilePath);
            throw;
        }
    }

    private void UpdateSettingsProperty<T>(Action<Settings, T> updateAction, T value)
    {
        try
        {
            var settings = GetSettings() ?? new Settings();
            updateAction(settings, value);
            SaveSettings(settings);
            _logger.Information("Successfully updated settings property");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating settings property with value: {Value}", value);
            throw;
        }
    }

    private void UpdateSettingsWithDefaults()
    {
        _logger.Information("Updating settings with default values for any missing fields");

        try
        {
            var settings = GetSettings() ?? new Settings();

            settings.IconLocation ??= "";
            settings.ConverterIconLocation ??= "";
            settings.ApplicationsLocation ??= "";
            settings.EnableDarkMode ??= false;
            settings.EnableLaunchAtStartup ??= false;
            settings.EnableAutoUpdate ??= true;
            settings.EnableSeasonalEffects ??= true;

            SaveSettings(settings);
            _logger.Information("Successfully updated settings with defaults");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating settings with defaults");
            throw;
        }
    }

    private void SaveSettings(Settings settings)
    {
        try
        {
            _logger.Information("Saving settings to file: {SettingsFilePath}", _settingsFilePath);
            var settingsData = SerializeSettings(settings);
            File.WriteAllText(_settingsFilePath, settingsData);
            _logger.Information("Successfully saved settings to file");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving settings to file: {SettingsFilePath}", _settingsFilePath);
            throw;
        }
    }

    private string SerializeSettings(Settings settings)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            return serialized;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error serializing settings");
            throw;
        }
    }

    private Settings? DeserializeSettings(string settingsData)
    {
        try
        {
            var deserialized = JsonSerializer.Deserialize<Settings>(settingsData);
            return deserialized;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error deserializing settings data");
            throw;
        }
    }
}