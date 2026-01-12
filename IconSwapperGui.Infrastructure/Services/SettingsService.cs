using System.Diagnostics;
using System.Text;
using System.Text.Json;
using IconSwapperGui.Core.Config;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly string _appDataPath;
    private readonly string _settingsPath;
    private readonly SemaphoreSlim _ioGate = new(1, 1);

    public SettingsService()
    {
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppInfo.SettingsFolderName
        );

        Directory.CreateDirectory(_appDataPath);

        _settingsPath = Path.Combine(_appDataPath, "settings.json");

        Settings = new AppSettings();
    }

    public AppSettings Settings { get; private set; }

    public event Action? SettingsChanged;

    public async Task LoadSettingsAsync()
    {
        await _ioGate.WaitAsync();
        try
        {
            if (!File.Exists(_settingsPath))
            {
                Settings = new AppSettings();
                NormalizeSettings(Settings);
                await SaveSettingsCoreAsync();
                SettingsChanged?.Invoke();
                return;
            }

            string json;
            try
            {
                json = await File.ReadAllTextAsync(_settingsPath, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Failed to read settings: {ex.Message}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Failed to read settings: {ex.Message}");
                return;
            }

            AppSettings? loadedSettings;
            try
            {
                loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Settings JSON is invalid: {ex.Message}");
                return;
            }

            if (loadedSettings is null)
                return;

            Settings = loadedSettings;
            NormalizeSettings(Settings);
            SettingsChanged?.Invoke();
        }
        finally
        {
            _ioGate.Release();
        }
    }

    private static void NormalizeSettings(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.Appearance ??= new();
        settings.Application ??= new();
        settings.General ??= new();
        settings.Notifications ??= new();
        settings.Advanced ??= new();

        settings.Application.ShortcutLocations ??= [];
        settings.Application.IconLocations ??= [];
        settings.Application.FolderShortcutLocations ??= [];
        settings.Application.ConverterIconsLocations ??= [];
        settings.Application.ExportLocation ??= string.Empty;
    }

    public async Task SaveSettingsAsync()
    {
        await _ioGate.WaitAsync();
        try
        {
            NormalizeSettings(Settings);
            await SaveSettingsCoreAsync();
            SettingsChanged?.Invoke();
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
        finally
        {
            _ioGate.Release();
        }
    }

    private async Task SaveSettingsCoreAsync()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };

        var json = JsonSerializer.Serialize(Settings, options);

        var tempPath = _settingsPath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, Encoding.UTF8);

        try
        {
            File.Replace(tempPath, _settingsPath, null);
        }
        catch (PlatformNotSupportedException)
        {
            File.Copy(tempPath, _settingsPath, overwrite: true);
            File.Delete(tempPath);
        }
    }

    public async Task ResetToDefaultsAsync()
    {
        Settings = new AppSettings();
        NormalizeSettings(Settings);
        await SaveSettingsAsync();
    }

    public string GetSettingsFilePath()
    {
        return _settingsPath;
    }

    public string GetAppDataPath()
    {
        return _appDataPath;
    }
}