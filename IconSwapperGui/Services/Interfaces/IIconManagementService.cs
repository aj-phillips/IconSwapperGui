using System.Collections.ObjectModel;
using IconSwapperGui.Models;

namespace IconSwapperGui.Services.Interfaces;

public interface IIconManagementService
{
    IEnumerable<Icon> GetIcons(string? folderPath, IEnumerable<string> extensions);
    ObservableCollection<Icon> GetIcons(string? folderPath);
    ObservableCollection<Icon> FilterIcons(ObservableCollection<Icon> icons, string? filterString);
}