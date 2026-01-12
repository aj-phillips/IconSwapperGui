namespace IconSwapperGui.Core.Models.Settings;

public class ApplicationSettings
{
    public List<string> ShortcutLocations { get; set; } = [];
    public List<string> IconLocations { get; set; } = [];

    public List<string> FolderShortcutLocations { get; set; } = [];

    public List<string> ConverterIconsLocations { get; set; } = [];

    public string ExportLocation { get; set; } = string.Empty;
}