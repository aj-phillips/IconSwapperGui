namespace IconSwapperGui.Core.Interfaces;

public interface IFileDialogService
{
    string? BrowseForFolder(string? initialPath, string title);
    IReadOnlyList<string> OpenFiles(string title, string filter, bool multiselect = true);
    string? SaveFile(string title, string fileName, string filter, string defaultExtension);
}
