using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace IconSwapperGui.UI.ViewModels;

public sealed partial class PixelArtLayerViewModel : ObservableObject
{
    private uint[] _pixelsArgb;

    public PixelArtLayerViewModel(string name, int rows, int columns)
    {
        _name = name;
        _pixelsArgb = new uint[checked(rows * columns)];
    }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private bool _isRenaming;

    public uint[] PixelsArgb => _pixelsArgb;

    public void Resize(int rows, int columns)
    {
        var newPixels = new uint[checked(rows * columns)];
        Array.Copy(_pixelsArgb, newPixels, Math.Min(_pixelsArgb.Length, newPixels.Length));
        _pixelsArgb = newPixels;
        OnPropertyChanged(nameof(PixelsArgb));
    }
}
