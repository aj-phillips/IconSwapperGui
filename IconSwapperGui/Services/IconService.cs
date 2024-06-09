using System.IO;
using IconSwapperGui.Interfaces;
using IconSwapperGui.Models;

namespace IconSwapperGui.Services 
{
    public class IconService : IIconService
    {
        public IEnumerable<Icon> GetIcons(string folderPath)
        {
            var icons = new List<Icon>();

            if (!Directory.Exists(folderPath)) throw new DirectoryNotFoundException($"Directory {folderPath} does not exist.");

            try
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.ico"))
                {
                    var icon = new Icon(Path.GetFileName(file), file);
                    icons.Add(icon);
                }

                return icons;
            }
            catch (IOException ex)
            {
                throw new IOException($"An error occurred while accessing {folderPath}: {ex.Message}", ex);
            }
        }
        
        public IEnumerable<Icon> GetPngIcons(string folderPath)
        {
            var icons = new List<Icon>();

            if (!Directory.Exists(folderPath)) throw new DirectoryNotFoundException($"Directory {folderPath} does not exist.");

            try
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.png"))
                {
                    var icon = new Icon(Path.GetFileName(file), file);
                    icons.Add(icon);
                }

                return icons;
            }
            catch (IOException ex)
            {
                throw new IOException($"An error occurred while accessing {folderPath}: {ex.Message}", ex);
            }
        }
    }
}