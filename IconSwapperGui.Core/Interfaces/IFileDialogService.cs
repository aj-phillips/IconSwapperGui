namespace IconSwapperGui.Core.Interfaces;

public interface IFileDialogService
{
    string? BrowseForFolder(string? initialPath, string title);
    string? SaveFile(string title, string fileName, string filter, string defaultExtension);
}
