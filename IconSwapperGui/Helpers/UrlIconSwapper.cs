using System;
using System.IO;

namespace IconSwapperGui.Helpers
{
    public class UrlIconSwapper
    {
        public void Swap(string applicationPath, string iconPath)
        {
            var urlFileContent = File.ReadAllLines(applicationPath);

            for (var i = 0; i < urlFileContent.Length; i++)
            {
                if (urlFileContent[i].StartsWith("IconFile", StringComparison.CurrentCultureIgnoreCase))
                {
                    urlFileContent[i] = "IconFile=" + iconPath;
                }
            }

            File.WriteAllLines(applicationPath, urlFileContent);
        }
    }
}