using IconSwapperGui.Models;

namespace IconSwapperGui.Services.Interfaces;

public interface IApplicationService
{
    IEnumerable<Application> GetApplications(string? folderPath);
}