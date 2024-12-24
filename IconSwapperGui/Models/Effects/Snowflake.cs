using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IconSwapperGui.Models.Effects;

public class Snowflake
{
    private readonly int _canvasHeight;
    private readonly int _canvasWidth;
    private readonly double _driftFrequency;
    private readonly double _lateralDriftAmplitude;
    private readonly double _speed;

    private double _initialX;

    public Snowflake(int canvasWidth, int canvasHeight)
    {
        _canvasWidth = canvasWidth;
        _canvasHeight = canvasHeight;

        Ellipse = new Ellipse
        {
            Width = Random.Shared.Next(3, 8),
            Height = Random.Shared.Next(3, 8),
            Fill = Brushes.White,
            Opacity = Random.Shared.NextDouble() * 0.5 + 1
        };

        RenderOptions.SetBitmapScalingMode(Ellipse, BitmapScalingMode.LowQuality);
        RenderOptions.SetCachingHint(Ellipse, CachingHint.Cache);

        ResetPosition();

        _speed = 0.75 + Random.Shared.NextDouble() * 1;
        _lateralDriftAmplitude = Random.Shared.Next(2, 5);
        _driftFrequency = 1 + Random.Shared.NextDouble();
    }

    public bool IsActive { get; set; } = true;

    public Ellipse Ellipse { get; }

    private double CurrentY => Canvas.GetTop(Ellipse);

    public static double GlobalElapsedTime { get; set; }

    public void UpdatePosition()
    {
        var newY = CurrentY + _speed;

        if (newY > _canvasHeight)
        {
            ResetPosition();
            return;
        }

        var drift = Math.Sin(GlobalElapsedTime * _driftFrequency) * _lateralDriftAmplitude;
        var newX = _initialX + drift;

        if (Canvas.GetTop(Ellipse) == newY && Canvas.GetLeft(Ellipse) == newX) return;

        Canvas.SetTop(Ellipse, newY);
        Canvas.SetLeft(Ellipse, newX);
    }

    public void ResetPosition()
    {
        _initialX = Random.Shared.Next(0, _canvasWidth);
        Canvas.SetLeft(Ellipse, _initialX);
        Canvas.SetTop(Ellipse, -Random.Shared.Next(0, 50));

        IsActive = true;
    }
}