using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Core.Interfaces;

public interface IUrlSwapperService
{
    IconOperationResult Swap(string applicationPath, string iconPath);
}
