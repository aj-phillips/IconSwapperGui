using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using IconSwapperGui.Models.Effects;

namespace IconSwapperGui.Themes.Effects;

public class WinterEffect : SeasonalEffectBase
{
    private const int SnowflakeCount = 100;

    private readonly Stopwatch _globalStopwatch = Stopwatch.StartNew();
    private readonly List<Snowflake> _snowflakePool = [];

    public override void Apply(Window window)
    {
        var canvas = new Canvas { IsHitTestVisible = false, Name = "EffectsCanvas" };
        var width = (int)window.Width;
        var height = (int)window.Height;

        for (var i = 0; i < SnowflakeCount; i++)
        {
            var snowflake = new Snowflake(width, height);
            _snowflakePool.Add(snowflake);
            canvas.Children.Add(snowflake.Ellipse);
        }

        if (window.Content is Grid grid)
            grid.Children.Add(canvas);

        CompositionTarget.Rendering -= OnRendering;
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        Snowflake.GlobalElapsedTime = _globalStopwatch.Elapsed.TotalSeconds;

        foreach (var snowflake in _snowflakePool)
            if (snowflake.IsActive)
                snowflake.UpdatePosition();
            else
                snowflake.ResetPosition();
    }

    public void RemoveEffect(Window window)
    {
        CompositionTarget.Rendering -= OnRendering;
        _snowflakePool.Clear();

        if (window.Content is Grid grid && grid.FindName("EffectsCanvas") is Canvas canvas)
            grid.Children.Remove(canvas);

        GC.WaitForPendingFinalizers();
    }
}