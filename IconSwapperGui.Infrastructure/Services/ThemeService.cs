using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Settings;

namespace IconSwapperGui.Infrastructure.Services;

public class ThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;

    public ThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        CurrentTheme = _settingsService.Settings.Appearance.Theme;
    }

    public ThemeMode CurrentTheme { get; private set; }

    public bool IsDarkMode => CurrentTheme == ThemeMode.Dark;

    public event Action? ThemeChanged;

    public void ApplyTheme(ThemeMode theme)
    {
        CurrentTheme = theme;
        ThemeChanged?.Invoke();
    }

    public void ToggleTheme()
    {
        var newTheme = CurrentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        ApplyTheme(newTheme);
    }
}