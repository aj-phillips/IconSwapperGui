using IconSwapperGui.Models;

namespace IconSwapperGui.Interfaces
{
    public interface ISettingsService
    {
        void SaveIconsLocation(string? iconsPath);
        void SaveConverterIconsLocation(string? iconsPath);
        void SaveApplicationsLocation(string? applicationsPath);
        void SaveEnableDarkMode(bool? enableDarkMode);
        Settings? GetSettings();

        string? GetApplicationsLocation();
        string? GetIconsLocation();
        string? GetConverterIconsLocation();
    }
}