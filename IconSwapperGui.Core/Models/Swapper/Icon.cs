namespace IconSwapperGui.Core.Models.Swapper;

public class Icon(string name, string path)
{
    public string Name { get; set; } = name;
    public string Path { get; set; } = path;
}