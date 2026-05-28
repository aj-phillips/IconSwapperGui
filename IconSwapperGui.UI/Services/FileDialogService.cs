using System.IO;
using IconSwapperGui.Core.Interfaces;
using Microsoft.Win32;

namespace IconSwapperGui.UI.Services;

public sealed class FileDialogService : IFileDialogService
{
    public string? BrowseForFolder(string? initialPath, string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
        {
            dialog.InitialDirectory = initialPath;
        }

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public IReadOnlyList<string> OpenFiles(string title, string filter, bool multiselect = true)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = multiselect
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : [];
    }

    public string? SaveFile(string title, string fileName, string filter, string defaultExtension)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = fileName,
            Filter = filter,
            DefaultExt = defaultExtension
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
