using System.Collections.ObjectModel;
using IconSwapperGui.Models;

namespace IconSwapperGui.Interfaces;

public interface IIconViewModel
{
    ObservableCollection<Icon> Icons { get; set; }
    string? IconsFolderPath { get; set; }
    ISettingsService SettingsService { get; set; }
    void PopulateIconsList(string? folderPath);
}