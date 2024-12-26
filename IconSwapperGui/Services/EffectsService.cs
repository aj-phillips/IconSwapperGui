using System.Windows;
using System.Windows.Controls;
using IconSwapperGui.Models.Effects;
using IconSwapperGui.Services.Interfaces;
using IconSwapperGui.Themes.Effects;

namespace IconSwapperGui.Services;

public class EffectsService : IEffectsService
{
    private readonly bool _enableSeasonalEffects;
    private SeasonalEffectBase? _currentEffect;

    public EffectsService(ISettingsService settingsService)
    {
        _enableSeasonalEffects = settingsService.GetSeasonalEffectsValue() ?? false;
    }

    public void ApplySeasonalEffect(Window window)
    {
        ClearEffects(window);

        if (!_enableSeasonalEffects) return;

        _currentEffect = GetSeasonalEffect();

        _currentEffect?.Apply(window);
    }

    public SeasonalEffectBase? GetSeasonalEffect()
    {
        var month = DateTime.Now.Month;

        return month switch
        {
            12 or 1 or 2 => new WinterEffect(),
            _ => null
        };
    }

    public void ClearEffects(Window window)
    {
        if (_currentEffect is WinterEffect winterEffect) winterEffect.RemoveEffect(window);

        if (window.Content is Grid grid && grid.FindName("EffectsCanvas") is Canvas canvas)
            grid.Children.Remove(canvas);

        _currentEffect = null;
    }
}