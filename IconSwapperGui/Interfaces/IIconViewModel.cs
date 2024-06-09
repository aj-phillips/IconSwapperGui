using System.Collections.ObjectModel;
using IconSwapperGui.Models;
using IconSwapperGui.Services;

namespace IconSwapperGui.Interfaces;

public interface IIconViewModel
{
    ObservableCollection<Icon> Icons { get; set; }
    string IconsFolderPath { get; set; }
    void PopulateIconsList(string folderPath);
    ISettingsService SettingsService { get; set; }
}