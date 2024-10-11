using System.IO;
using System.Text.Json;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;
using Newtonsoft.Json.Linq;

namespace IconSwapperGui.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            EnsureSettingsFileExists();
            UpdateSettingsWithDefaults();
        }

        private void EnsureSettingsFileExists()
        {
            if (File.Exists(_settingsFilePath)) return;

            var defaultSettings = new Settings
            {
                IconLocation = "",
                ConverterIconLocation = "",
                ApplicationsLocation = "",
                EnableDarkMode = false,
                EnableLaunchAtStartup = false
            };

            SaveSettings(defaultSettings);
        }

        public Settings? GetSettings()
        {
            if (!File.Exists(_settingsFilePath)) return null;

            var settingsData = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<Settings>(settingsData);
        }
        
        public T GetSettingsFieldValue<T>(string fieldName)
        {
            var settings = JObject.Parse(File.ReadAllText(_settingsFilePath));
            if (settings.TryGetValue(fieldName, out JToken fieldValue))
            {
                return fieldValue.ToObject<T>();
            }

            return default;
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

            SaveSettings(settings);
        }
        
        private void SaveSettings(Settings settings)
        {
            var settingsData = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, settingsData);
        }

        public void SaveIconsLocation(string? iconsPath) =>
            UpdateSettingsProperty((settings, value) => settings.IconLocation = value, iconsPath);

        public void SaveConverterIconsLocation(string? iconsPath) =>
            UpdateSettingsProperty((settings, value) => settings.ConverterIconLocation = value, iconsPath);

        public void SaveApplicationsLocation(string? applicationsPath) =>
            UpdateSettingsProperty((settings, value) => settings.ApplicationsLocation = value, applicationsPath);

        public void SaveEnableDarkMode(bool? enableDarkMode) =>
            UpdateSettingsProperty((settings, value) => settings.EnableDarkMode = value, enableDarkMode);

        public void SaveEnableLaunchAtStartup(bool? enableLaunchAtStartup) =>
            UpdateSettingsProperty((settings, value) => settings.EnableLaunchAtStartup = value, enableLaunchAtStartup);

        public string? GetApplicationsLocation() => GetSettingsFieldValue<string>("ApplicationsLocation");

        public string? GetIconsLocation() => GetSettingsFieldValue<string>("IconLocation");

        public string? GetConverterIconsLocation() => GetSettingsFieldValue<string>("ConverterIconLocation");
    }
}