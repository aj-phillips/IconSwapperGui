using IconSwapperGui.Core.Models;
using IconSwapperGui.Core.Models.Settings;

namespace IconSwapperGui.Core.Interfaces;

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    bool IsDarkMode { get; }

    void ApplyTheme(ThemeMode theme);
    void ApplyAccentColor(string colorHex);
    void ToggleTheme();

    event Action? ThemeChanged;
}