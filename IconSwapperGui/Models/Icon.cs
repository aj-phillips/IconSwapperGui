namespace IconSwapperGui.Models;

public class Icon
{
    public string Name { get; set; }
    public string Path { get; set; }

    public Icon(string name, string path)
    {
        Name = name;
        Path = path;
    }
}