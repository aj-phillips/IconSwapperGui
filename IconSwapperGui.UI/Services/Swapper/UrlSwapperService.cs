using System.IO;
using IconSwapperGui.Core.Interfaces;

namespace IconSwapperGui.UI.Services.Swapper;

public class UrlSwapperService : IUrlSwapperService
{
    public void Swap(string applicationPath, string iconPath)
    {
        var urlFileContent = File.ReadAllLines(applicationPath).ToList();

        var replaced = false;

        for (var i = 0; i < urlFileContent.Count; i++)
        {
            if (urlFileContent[i].StartsWith("IconFile", StringComparison.CurrentCultureIgnoreCase))
            {
                urlFileContent[i] = "IconFile=" + iconPath;
                replaced = true;
            }
        }

        if (!replaced)
        {
            urlFileContent.Add("IconFile=" + iconPath);
        }

        File.WriteAllLines(applicationPath, urlFileContent);
    }
}