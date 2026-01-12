using System.Windows;
using System.Windows.Media;
using IconSwapperGui.Core.Interfaces;
using ThemeMode = IconSwapperGui.Core.Models.Settings.ThemeMode;

namespace IconSwapperGui.UI.Services;

public class ThemeApplier : IDisposable
{
    private readonly IDispatcher _dispatcher;
    private readonly IThemeService _themeService;
    private readonly ISettingsService _settingsService;

    public ThemeApplier(IThemeService themeService, ISettingsService settingsService, IDispatcher dispatcher)
    {
        _themeService = themeService;
        _settingsService = settingsService;
        _dispatcher = dispatcher;
        _themeService.ThemeChanged += OnThemeChanged;

        // Apply initial theme
        Apply(_themeService.CurrentTheme);
    }

    public void Dispose()
    {
        _themeService.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged()
    {
        Apply(_themeService.CurrentTheme);
    }

    private void Apply(ThemeMode theme)
    {
        _dispatcher.Invoke(() =>
        {
            var app = Application.Current;
            if (app == null) return;

            var themeDictionaries = app.Resources.MergedDictionaries
                .Where(d => d.Source?.OriginalString?.Contains("LightTheme.xaml") == true ||
                            d.Source?.OriginalString?.Contains("DarkTheme.xaml") == true ||
                            d.Source?.OriginalString?.Contains("CustomTheme.xaml") == true)
                .ToList();

            foreach (var dict in themeDictionaries) app.Resources.MergedDictionaries.Remove(dict);

            if (theme != ThemeMode.Custom)
            {
                ClearCustomColorOverrides();
            }

            var themeUri = theme switch
            {
                ThemeMode.Dark => "Themes/DarkTheme.xaml",
                ThemeMode.Custom => "Themes/CustomTheme.xaml",
                _ => "Themes/LightTheme.xaml"
            };

            try
            {
                var themeDict = new ResourceDictionary
                {
                    Source = new Uri(themeUri, UriKind.Relative)
                };
                app.Resources.MergedDictionaries.Add(themeDict);

                if (theme == ThemeMode.Custom)
                {
                    ApplyCustomColors();
                }
            }
            catch
            {
                // ignore
            }
        });
    }

    private void ClearCustomColorOverrides()
    {
        var app = Application.Current;
        if (app == null) return;

        var customColorKeys = new[]
        {
            "AccentBrush",
            "AccentHoverBrush",
            "AccentLightBrush",
            "AppBackgroundBrush",
            "SurfaceBackgroundBrush",
            "SidebarBackgroundBrush",
            "PrimaryTextBrush",
            "SecondaryTextBrush"
        };

        foreach (var key in customColorKeys)
        {
            if (app.Resources.Contains(key))
            {
                app.Resources.Remove(key);
            }
        }
    }

    private void ApplyCustomColors()
    {
        var settings = _settingsService.Settings.Appearance;

        ApplyColorIfSet(settings.CustomAccentColor, "AccentBrush", true);
        ApplyColorIfSet(settings.CustomBackgroundColor, "AppBackgroundBrush");
        ApplyColorIfSet(settings.CustomSurfaceColor, "SurfaceBackgroundBrush");
        ApplyColorIfSet(settings.CustomSurfaceColor, "SidebarBackgroundBrush");
        ApplyColorIfSet(settings.CustomPrimaryTextColor, "PrimaryTextBrush");
        ApplyColorIfSet(settings.CustomSecondaryTextColor, "SecondaryTextBrush");
    }

    private void ApplyColorIfSet(string? colorHex, string resourceKey, bool withVariants = false)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
            return;

        var app = Application.Current;
        if (app == null) return;

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorHex);
            var brush = new SolidColorBrush(color);
            brush.Freeze();

            app.Resources[resourceKey] = brush;

            if (withVariants && resourceKey == "AccentBrush")
            {
                var hoverColor = Color.FromArgb(color.A,
                    (byte)Math.Max(0, color.R - 20),
                    (byte)Math.Max(0, color.G - 20),
                    (byte)Math.Max(0, color.B - 20));
                var hoverBrush = new SolidColorBrush(hoverColor);
                hoverBrush.Freeze();
                app.Resources["AccentHoverBrush"] = hoverBrush;

                var lightColor = Color.FromArgb(40, color.R, color.G, color.B);
                var lightBrush = new SolidColorBrush(lightColor);
                lightBrush.Freeze();
                app.Resources["AccentLightBrush"] = lightBrush;
            }
        }
        catch
        {
            // ignore invalid color
        }
    }
}