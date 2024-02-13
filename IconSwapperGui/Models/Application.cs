﻿namespace IconSwapperGui.Models;

public class Application
{
    public string Name { get; set; }
    public string Path { get; set; }

    public Application(string name, string path)
    {
        Name = name;
        Path = path;
    }
}