using IconSwapperGui.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IconSwapperGui.Interfaces
{
    public interface IIconManagementService
    {
        IEnumerable<Icon> GetIcons(string folderPath, IEnumerable<string> extensions);
        ObservableCollection<Icon> GetIcons(string folderPath);
        ObservableCollection<Icon> FilterIcons(ObservableCollection<Icon> icons, string filterString);
    }
}