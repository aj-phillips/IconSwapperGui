using System.Windows.Media;

namespace IconSwapperGui.UI.Services.IconTools;

public sealed class FolderThemeRenderOptions
{
    public Color FolderColor { get; init; }

    public Color AccentColor { get; init; }

    public string Badge { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public bool ShowBadge { get; init; } = true;

    public bool ShowDocument { get; init; } = true;

    public string StyleName { get; init; } = "Windows";

    public string TabStyleName { get; init; } = "Angled";

    public string LabelPlacementName { get; init; } = "Front";

    public double LabelFontSize { get; init; } = 24;

    public double Depth { get; init; } = 0.65;

    public double CornerSoftness { get; init; } = 10;
}
