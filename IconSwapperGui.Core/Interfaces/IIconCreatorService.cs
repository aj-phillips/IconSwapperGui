namespace IconSwapperGui.Core.Interfaces;

public interface IIconCreatorService
{
    void CreateIcoFromPng(string pngPath, string icoPath, int targetSize = 128);
    void CreateMultiSizeIcoFromImage(string sourceImagePath, string icoPath, int[] sizes);
}