using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;

namespace IconSwapperGui.UI.Views.Components;

public partial class ColorPickerPopup : UserControl
{
    private bool _isDraggingSv;
    private bool _isDraggingHue;
    public static readonly DependencyProperty SelectedColorProperty =
        DependencyProperty.Register(nameof(SelectedColor), typeof(Color), typeof(ColorPickerPopup),
            new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public static readonly DependencyProperty IsCustomTabSelectedProperty =
        DependencyProperty.Register(nameof(IsCustomTabSelected), typeof(bool), typeof(ColorPickerPopup),
            new PropertyMetadata(false));

    public bool IsCustomTabSelected
    {
        get => (bool)GetValue(IsCustomTabSelectedProperty);
        set => SetValue(IsCustomTabSelectedProperty, value);
    }

    public static readonly DependencyProperty RecentColorsProperty =
        DependencyProperty.Register(nameof(RecentColors), typeof(IEnumerable), typeof(ColorPickerPopup),
            new PropertyMetadata(null));

    public IEnumerable? RecentColors
    {
        get => (IEnumerable?)GetValue(RecentColorsProperty);
        set => SetValue(RecentColorsProperty, value);
    }

    public static readonly DependencyProperty AutoCloseOnPickProperty =
        DependencyProperty.Register(nameof(AutoCloseOnPick), typeof(bool), typeof(ColorPickerPopup),
            new PropertyMetadata(true));

    public bool AutoCloseOnPick
    {
        get => (bool)GetValue(AutoCloseOnPickProperty);
        set => SetValue(AutoCloseOnPickProperty, value);
    }

    public static readonly DependencyProperty HueProperty =
        DependencyProperty.Register(nameof(Hue), typeof(double), typeof(ColorPickerPopup),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHsvChanged));

    public static readonly DependencyProperty HueColorProperty =
        DependencyProperty.Register(nameof(HueColor), typeof(Color), typeof(ColorPickerPopup),
            new PropertyMetadata(Colors.Red));

    public Color HueColor
    {
        get => (Color)GetValue(HueColorProperty);
        set => SetValue(HueColorProperty, value);
    }

    public static readonly DependencyProperty SaturationProperty =
        DependencyProperty.Register(nameof(Saturation), typeof(double), typeof(ColorPickerPopup),
            new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHsvChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(ColorPickerPopup),
            new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHsvChanged));

    public double Hue
    {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public double Saturation
    {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public event EventHandler? ColorPicked;
    public event EventHandler? ColorCommitted;

    private void RaiseColorPickedIfEnabled()
    {
        if (!AutoCloseOnPick) return;
        ColorPicked?.Invoke(this, EventArgs.Empty);
    }

    private void RaiseColorCommitted()
    {
        ColorCommitted?.Invoke(this, EventArgs.Empty);
    }

    public ColorPickerPopup()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            SyncHsvFromSelectedColor();
            UpdateThumbs();
        };

        SizeChanged += (_, _) => UpdateThumbs();
    }

    private void SvSurface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSv = true;
        ((UIElement)sender).CaptureMouse();
        UpdateSaturationValueFromPoint(e.GetPosition((IInputElement)sender));
    }

    private void SvSurface_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingSv) return;
        UpdateSaturationValueFromPoint(e.GetPosition((IInputElement)sender));
    }

    private void SvSurface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingSv) return;
        _isDraggingSv = false;
        ((UIElement)sender).ReleaseMouseCapture();
        RaiseColorCommitted();
        RaiseColorPickedIfEnabled();
    }

    private void HueStrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingHue = true;
        ((UIElement)sender).CaptureMouse();
        UpdateHueFromPoint(e.GetPosition((IInputElement)sender));
    }

    private void HueStrip_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingHue) return;
        UpdateHueFromPoint(e.GetPosition((IInputElement)sender));
    }

    private void HueStrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingHue) return;
        _isDraggingHue = false;
        ((UIElement)sender).ReleaseMouseCapture();
        RaiseColorCommitted();
        RaiseColorPickedIfEnabled();
    }

    private void UpdateSaturationValueFromPoint(Point p)
    {
        if (SvSurface is null) return;

        var width = SvSurface.ActualWidth;
        var height = SvSurface.ActualHeight;
        if (width <= 0 || height <= 0) return;

        var x = Math.Clamp(p.X, 0, width);
        var y = Math.Clamp(p.Y, 0, height);

        Saturation = x / width;
        Value = 1 - (y / height);
        UpdateThumbs();
    }

    private void UpdateHueFromPoint(Point p)
    {
        if (HueSurface is null) return;

        var height = HueSurface.ActualHeight;
        if (height <= 0) return;

        var y = Math.Clamp(p.Y, 0, height);
        Hue = (y / height) * 360d;
        if (Hue >= 360d) Hue = 0d;
        UpdateThumbs();
    }

    private void OnChipClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not string hex) return;

        if (TryParseHexColor(hex, out var color))
        {
            SelectedColor = color;
            SyncHsvFromSelectedColor();
            RaiseColorCommitted();
            RaiseColorPickedIfEnabled();
        }
    }

    private void OnRecentClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not Color c) return;
        SelectedColor = c;
        SyncHsvFromSelectedColor();
        RaiseColorCommitted();
        RaiseColorPickedIfEnabled();
    }

    private static void OnHsvChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ColorPickerPopup picker) return;
        picker.SyncSelectedColorFromHsv();
    }

    private void SyncSelectedColorFromHsv()
    {
        var (hr, hg, hb) = HsvToRgb(Hue, 1, 1);
        HueColor = Color.FromArgb(0xFF, hr, hg, hb);
        var (r, g, b) = HsvToRgb(Hue, Saturation, Value);
        SelectedColor = Color.FromArgb(0xFF, r, g, b);
        UpdateThumbs();
    }

    private void SyncHsvFromSelectedColor()
    {
        var (h, s, v) = RgbToHsv(SelectedColor);
        Hue = h;
        Saturation = s;
        Value = v;
        var (hr, hg, hb) = HsvToRgb(Hue, 1, 1);
        HueColor = Color.FromArgb(0xFF, hr, hg, hb);
        UpdateThumbs();
    }

    private void UpdateThumbs()
    {
        if (SvSurface is not null && SvThumb is not null)
        {
            var w = SvSurface.ActualWidth;
            var h = SvSurface.ActualHeight;
            if (w > 0 && h > 0)
            {
                var x = Math.Clamp(Saturation, 0, 1) * w;
                var y = (1 - Math.Clamp(Value, 0, 1)) * h;

                var tw = SvThumb.ActualWidth > 0 ? SvThumb.ActualWidth : 14;
                var th = SvThumb.ActualHeight > 0 ? SvThumb.ActualHeight : 14;
                var left = Math.Clamp(x - (tw / 2), 0, Math.Max(0, w - tw));
                var top = Math.Clamp(y - (th / 2), 0, Math.Max(0, h - th));

                Canvas.SetLeft(SvThumb, left);
                Canvas.SetTop(SvThumb, top);
            }
        }

        if (HueSurface is not null && HueThumb is not null)
        {
            var h = HueSurface.ActualHeight;
            if (h > 0)
            {
                var y = (Math.Clamp(Hue, 0, 360) / 360d) * h;

                var tw = HueThumb.ActualWidth > 0 ? HueThumb.ActualWidth : 14;
                var th = HueThumb.ActualHeight > 0 ? HueThumb.ActualHeight : 14;
                var left = Math.Clamp((HueSurface.ActualWidth / 2) - (tw / 2), 0, Math.Max(0, HueSurface.ActualWidth - tw));
                var top = Math.Clamp(y - (th / 2), 0, Math.Max(0, h - th));

                Canvas.SetLeft(HueThumb, left);
                Canvas.SetTop(HueThumb, top);
            }
        }
    }

    private static (byte r, byte g, byte b) HsvToRgb(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);

        if (saturation <= 0)
        {
            var v = (byte)Math.Round(value * 255);
            return (v, v, v);
        }

        var c = value * saturation;
        var x = c * (1 - Math.Abs((hue / 60d % 2) - 1));
        var m = value - c;

        double r1, g1, b1;
        if (hue < 60) (r1, g1, b1) = (c, x, 0);
        else if (hue < 120) (r1, g1, b1) = (x, c, 0);
        else if (hue < 180) (r1, g1, b1) = (0, c, x);
        else if (hue < 240) (r1, g1, b1) = (0, x, c);
        else if (hue < 300) (r1, g1, b1) = (x, 0, c);
        else (r1, g1, b1) = (c, 0, x);

        var r = (byte)Math.Round((r1 + m) * 255);
        var g = (byte)Math.Round((g1 + m) * 255);
        var b = (byte)Math.Round((b1 + m) * 255);
        return (r, g, b);
    }

    private static (double h, double s, double v) RgbToHsv(Color c)
    {
        var r = c.R / 255d;
        var g = c.G / 255d;
        var b = c.B / 255d;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        double h;
        if (delta == 0) h = 0;
        else if (max == r) h = 60 * (((g - b) / delta) % 6);
        else if (max == g) h = 60 * (((b - r) / delta) + 2);
        else h = 60 * (((r - g) / delta) + 4);

        if (h < 0) h += 360;

        var s = max == 0 ? 0 : delta / max;
        var v = max;
        return (h, s, v);
    }

    private static bool TryParseHexColor(string hex, out Color color)
    {
        color = Colors.Transparent;

        if (string.IsNullOrWhiteSpace(hex)) return false;

        hex = hex.Trim();
        if (hex.StartsWith("#", StringComparison.Ordinal)) hex = hex[1..];

        if (hex.Length is not (6 or 8)) return false;

        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) return false;

        if (hex.Length == 6)
        {
            var r = (byte)((value >> 16) & 0xFF);
            var g = (byte)((value >> 8) & 0xFF);
            var b = (byte)(value & 0xFF);
            color = Color.FromArgb(0xFF, r, g, b);
            return true;
        }
        else
        {
            var a = (byte)((value >> 24) & 0xFF);
            var r = (byte)((value >> 16) & 0xFF);
            var g = (byte)((value >> 8) & 0xFF);
            var b = (byte)(value & 0xFF);
            color = Color.FromArgb(a, r, g, b);
            return true;
        }
    }
}
