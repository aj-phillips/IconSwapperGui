using System.Windows;
using IconSwapperGui.Core.Interfaces;
using ThemeMode = IconSwapperGui.Core.Models.Settings.ThemeMode;

namespace IconSwapperGui.UI.Services;

public class ThemeApplier : IDisposable
{
    private readonly IDispatcher _dispatcher;
    private readonly IThemeService _themeService;

    public ThemeApplier(IThemeService themeService, IDispatcher dispatcher)
    {
        _themeService = themeService;
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
                .Where(d => d.Source?.OriginalString?.Contains("Theme") == true)
                .ToList();

            foreach (var dict in themeDictionaries) app.Resources.MergedDictionaries.Remove(dict);

            var themeUri = theme == ThemeMode.Dark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml";

            try
            {
                var themeDict = new ResourceDictionary
                {
                    Source = new Uri(themeUri, UriKind.Relative)
                };
                app.Resources.MergedDictionaries.Add(themeDict);
            }
            catch
            {
                // ignore
            }
        });
    }
}