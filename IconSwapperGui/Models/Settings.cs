namespace IconSwapperGui.Models;

public class Settings
{
    public string? ExportLocation { get; set; }
    public string? IconLocation { get; set; }
    public string? FoldersLocation { get; set; }
    public string? ConverterIconLocation { get; set; }
    public string? ApplicationsLocation { get; set; }
    public bool? EnableDarkMode { get; set; }
    public bool? EnableLaunchAtStartup { get; set; }
    public bool? EnableAutoUpdate { get; set; }
    public bool? EnableSeasonalEffects { get; set; }
}