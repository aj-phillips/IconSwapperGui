using System.IO;

namespace IconSwapperGui.Helpers;

public class UrlIconSwapper
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