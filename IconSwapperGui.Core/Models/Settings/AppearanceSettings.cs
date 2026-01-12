namespace IconSwapperGui.Core.Models.Settings;

public class AppearanceSettings
{
    public ThemeMode Theme { get; set; } = ThemeMode.Light;
    public bool UseSystemTheme { get; set; } = false;
    public string? CustomAccentColor { get; set; }
    public string? CustomBackgroundColor { get; set; }
    public string? CustomSurfaceColor { get; set; }
    public string? CustomPrimaryTextColor { get; set; }
    public string? CustomSecondaryTextColor { get; set; }
}

public enum ThemeMode
{
    Light,
    Dark,
    Custom,
    System
}