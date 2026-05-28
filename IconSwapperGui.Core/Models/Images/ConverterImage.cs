using IconSwapperGui.Core.Models.Swapper;

namespace IconSwapperGui.Core.Models.Images;

public class ConverterImage : Icon
{
    public ConverterImage(string name, string path, bool isManual = false) : base(name, path)
    {
        IsManual = isManual;
    }

    public bool IsManual { get; set; }

    public ImageTransformOptions TransformOptions { get; set; } = new();
}
