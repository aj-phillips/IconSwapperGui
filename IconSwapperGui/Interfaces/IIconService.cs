using IconSwapperGui.Models;

namespace IconSwapperGui.Interfaces;

public interface IIconService
{
    IEnumerable<Icon> GetIcons(string folderPath);
}