using IconSwapperGui.Models;

namespace IconSwapperGui.Services;

public interface IApplicationService
{
    IEnumerable<Application> GetApplications(string folderPath);
}