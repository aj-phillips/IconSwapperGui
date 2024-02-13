using IconSwapperGui.Models;

namespace IconSwapperGui.Services;

public interface IIconService
{
    IEnumerable<Icon> GetIcons(string folderPath);
}