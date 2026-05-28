namespace IconSwapperGui.Core.Models.Images;

public sealed class ImageTransformOptions
{
    public ImageCropMode CropMode { get; init; } = ImageCropMode.CenterCrop;

    public bool UseTransparentPadding { get; init; } = true;

    public double Zoom { get; init; } = 1.0;

    public double FocusX { get; init; } = 0.5;

    public double FocusY { get; init; } = 0.5;
}
