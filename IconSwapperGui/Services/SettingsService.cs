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

    public static event Action? LocationsChanged;

    public static void TriggerLocationsChanged()
    {
        try
        {
            LocationsChanged?.Invoke();
        }
        catch
        {
            // ignore
        }
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
        var currentSingle = GetIconsLocation();
        if (string.IsNullOrWhiteSpace(currentSingle))
        {
            UpdateSettingsProperty((settings, value) => settings.IconLocation = value, iconsPath);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(iconsPath))
            {
                var list = GetIconsLocations() ?? new List<string>();
                if (!list.Contains(iconsPath))
                {
                    list.Add(iconsPath);
                    SaveIconsLocations(list);
                }
            }
        }
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

    public void SaveFoldersLocation(string? foldersPath)
    {
        _logger.Information("Saving folders location: {FoldersPath}", foldersPath ?? "null");
        var currentSingle = GetFoldersLocation();
        if (string.IsNullOrWhiteSpace(currentSingle))
        {
            UpdateSettingsProperty((settings, value) => settings.FoldersLocation = value, foldersPath);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(foldersPath)) return;

            var list = GetFoldersLocations() ?? new List<string>();

            if (list.Contains(foldersPath)) return;

            list.Add(foldersPath);
            UpdateSettingsProperty((settings, value) => settings.FoldersLocations = value, list);
        }
    }

    public void SaveApplicationsLocation(string? applicationsPath)
    {
        _logger.Information("Saving applications location: {ApplicationsPath}", applicationsPath ?? "null");
        var currentSingle = GetApplicationsLocation();
        if (string.IsNullOrWhiteSpace(currentSingle))
        {
            UpdateSettingsProperty((settings, value) => settings.ApplicationsLocation = value, applicationsPath);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(applicationsPath)) return;

            var list = GetApplicationsLocations() ?? new List<string>();

            if (list.Contains(applicationsPath)) return;

            list.Add(applicationsPath);
            SaveApplicationsLocations(list);
        }
    }

    public void SaveIconsLocations(List<string>? iconsPaths)
    {
        _logger.Information("Saving icons locations (list)");
        var normalized = NormalizeLocations(iconsPaths);
        UpdateSettingsProperty((settings, value) => settings.IconLocations = value, normalized);
    }

    public void SaveApplicationsLocations(List<string>? applicationsPaths)
    {
        _logger.Information("Saving applications locations (list)");
        var normalized = NormalizeLocations(applicationsPaths);
        UpdateSettingsProperty((settings, value) => settings.ApplicationsLocations = value, normalized);
    }

    public void SaveFoldersLocations(List<string>? foldersPaths)
    {
        _logger.Information("Saving folders locations (list)");
        var normalized = NormalizeLocations(foldersPaths);
        UpdateSettingsProperty((settings, value) => settings.FoldersLocations = value, normalized);
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

        try
        {
            var full = Path.GetFullPath(path.Trim());
            full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full;
        }
        catch
        {
            return path.Trim();
        }
    }

    public List<string> NormalizeLocations(IEnumerable<string>? locations)
    {
        if (locations == null) return new List<string>();
        var normalized = locations
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(NormalizePath)
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return normalized;
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

    public List<string>? GetApplicationsLocations()
    {
        var locations = GetSettingsFieldValue<List<string>>("ApplicationsLocations");
        _logger.Information("Retrieved applications locations count: {Count}", locations?.Count ?? 0);
        return locations;
    }

    public string? GetIconsLocation()
    {
        var location = GetSettingsFieldValue<string>("IconLocation");
        _logger.Information("Retrieved icons location: {Location}", location ?? "null");
        return location;
    }

    public List<string>? GetIconsLocations()
    {
        var locations = GetSettingsFieldValue<List<string>>("IconLocations");
        _logger.Information("Retrieved icons locations count: {Count}", locations?.Count ?? 0);
        return locations;
    }

    public string? GetConverterIconsLocation()
    {
        var location = GetSettingsFieldValue<string>("ConverterIconLocation");
        _logger.Information("Retrieved converter icons location: {Location}", location ?? "null");
        return location;
    }

    public string? GetFoldersLocation()
    {
        var location = GetSettingsFieldValue<string>("FoldersLocation");
        _logger.Information("Retrieved folders location: {Location}", location ?? "null");
        return location;
    }

    public List<string>? GetFoldersLocations()
    {
        var locations = GetSettingsFieldValue<List<string>>("FoldersLocations");
        _logger.Information("Retrieved folders locations count: {Count}", locations?.Count ?? 0);
        return locations;
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
            IconLocations = new List<string>(),
            FoldersLocation = "",
            ConverterIconLocation = "",
            ApplicationsLocation = "",
            ApplicationsLocations = new List<string>(),
            EnableDarkMode = true,
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
            settings.IconLocations ??= new List<string>();
            settings.ConverterIconLocation ??= "";
            settings.ApplicationsLocation ??= "";
            settings.ApplicationsLocations ??= new List<string>();
            settings.EnableDarkMode ??= true;
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