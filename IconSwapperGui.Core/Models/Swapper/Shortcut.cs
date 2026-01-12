namespace IconSwapperGui.Core.Models.Swapper;

public class Shortcut(string name, string path, string? defaultTargetPath, string? targetPath)
{
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
    public string? DefaultTargetPath { get; set; } = defaultTargetPath;
    public string? TargetPath { get; set; } = targetPath;

    public Shortcut(string name, string path) : this(name, path, null, null)
    {
    }
}