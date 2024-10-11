namespace IconSwapperGui.Models;

public class Icon
{
    public Icon(string name, string path)
    {
        Name = name;
        Path = path;
    }

    public string Name { get; set; }
    public string Path { get; set; }
}