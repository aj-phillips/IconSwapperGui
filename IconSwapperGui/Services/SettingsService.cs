using System.IO;
using System.Text.Json;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;

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

            var settings = new Settings
            {
                IconLocation = "",
                ConverterIconLocation = "",
                ApplicationsLocation = "",
                EnableDarkMode = false
            };

            SaveSettings(settings);
        }

        public Settings? GetSettings()
        {
            if (!File.Exists(_settingsFilePath)) return null;

            var settingsData = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<Settings>(settingsData);
        }
        
        private void SaveSettings(Settings settings)
        {
            var settingsData = JsonSerializer.Serialize(settings);
            File.WriteAllText(_settingsFilePath, settingsData);
        }

        private void UpdateSettingsWithDefaults()
        {
            var settings = GetSettings() ?? new Settings();
            
            settings.IconLocation ??= "";
            settings.ConverterIconLocation ??= "";
            settings.ApplicationsLocation ??= "";
            settings.EnableDarkMode ??= false;

            SaveSettings(settings);
        }

        private void UpdateSettingsProperty<T>(Action<Settings, T> updateAction, T value)
        {
            var settings = GetSettings() ?? new Settings();

            updateAction(settings, value);
            SaveSettings(settings);
        }

        public void SaveIconsLocation(string? iconsPath) =>
            UpdateSettingsProperty((settings, value) => settings.IconLocation = value, iconsPath);

        public void SaveConverterIconsLocation(string? iconsPath) =>
            UpdateSettingsProperty((settings, value) => settings.ConverterIconLocation = value, iconsPath);

        public void SaveApplicationsLocation(string? applicationsPath) =>
            UpdateSettingsProperty((settings, value) => settings.ApplicationsLocation = value, applicationsPath);

        public void SaveEnableDarkMode(bool? enableDarkMode) =>
            UpdateSettingsProperty((settings, value) => settings.EnableDarkMode = value, enableDarkMode);

        public string? GetApplicationsLocation() => GetSettings()?.ApplicationsLocation;

        public string? GetIconsLocation() => GetSettings()?.IconLocation;

        public string? GetConverterIconsLocation() => GetSettings()?.ConverterIconLocation;
    }
}