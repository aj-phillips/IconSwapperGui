using System.Windows;
using IconSwapperGui.Models.Effects;

namespace IconSwapperGui.Services.Interfaces;

public interface IEffectsService
{
    public void ApplySeasonalEffect(Window window);
    public SeasonalEffectBase? GetSeasonalEffect();
    public void ClearEffects(Window window);
}