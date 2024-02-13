using IconSwapperGui.Models;
using System.IO;

namespace IconSwapperGui.Services;

public class ApplicationService : IApplicationService
{
    public IEnumerable<Application> GetApplications(string folderPath)
    {
        var applications = new List<Application>();

        try
        {
            if (Directory.Exists(folderPath))
            {
                var shortcutFiles = Directory.GetFiles(folderPath, "*.lnk");

                foreach (var file in shortcutFiles)
                {
                    var appName = Path.GetFileNameWithoutExtension(file);
                    var app = new Application(appName, file);

                    applications.Add(app);
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"An error occurred while accessing {folderPath}: {ex.Message}");
        }

        return applications;
    }
}