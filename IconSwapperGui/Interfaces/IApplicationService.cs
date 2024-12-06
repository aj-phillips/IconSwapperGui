using IconSwapperGui.Models;

namespace IconSwapperGui.Interfaces;

public interface IApplicationService
{
    IEnumerable<Application> GetApplications(string? folderPath);
}