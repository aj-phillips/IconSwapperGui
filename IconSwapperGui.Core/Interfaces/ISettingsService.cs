using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Core.Interfaces;

public interface ISettingsService
{
    AppSettings Settings { get; }

    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    Task ResetToDefaultsAsync();
    string GetSettingsFilePath();
    string GetAppDataPath();

    event Action? SettingsChanged;
}