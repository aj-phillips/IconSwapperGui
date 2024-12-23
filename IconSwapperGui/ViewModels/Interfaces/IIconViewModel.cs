using System.Collections.ObjectModel;
using IconSwapperGui.Models;
using IconSwapperGui.Services.Interfaces;

namespace IconSwapperGui.ViewModels.Interfaces;

public interface IIconViewModel
{
    ObservableCollection<Icon> Icons { get; set; }
    string? IconsFolderPath { get; set; }
    ISettingsService SettingsService { get; set; }
    void PopulateIconsList(string? folderPath);
}