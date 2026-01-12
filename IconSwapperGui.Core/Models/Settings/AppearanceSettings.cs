namespace IconSwapperGui.Core.Models.Settings;

public class AppearanceSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.Light;
    public bool UseSystemTheme { get; set; } = false;
}

public enum ThemeMode
{
    Light,
    Dark,
    System
}