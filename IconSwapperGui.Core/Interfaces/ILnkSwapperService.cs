using IconSwapperGui.Core.Models;

namespace IconSwapperGui.Core.Interfaces;

public interface ILnkSwapperService
{
    IconOperationResult Swap(string applicationPath, string iconPath, string applicationName);
    IconOperationResult RestoreDefault(string applicationPath, string applicationName);
}
