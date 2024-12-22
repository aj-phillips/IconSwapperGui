namespace IconSwapperGui.Models;

public class Application
{
    public Application(string name, string path)
    {
        Name = name;
        Path = path;
    }

    public Application(string name, string path, string defaultTargetPath, string targetPath)
    {
        Name = name;
        Path = path;
        DefaultTargetPath = defaultTargetPath;
        TargetPath = targetPath;
    }

    public string Name { get; set; }
    public string Path { get; set; }
    public string? DefaultTargetPath { get; set; }
    public string? TargetPath { get; set; }
}