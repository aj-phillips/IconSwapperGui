using IconSwapperGui.Core.Models.Settings;

namespace IconSwapperGui.Core.Models;

public class AppSettings
{
    public AppearanceSettings Appearance { get; set; } = new();

    public ApplicationSettings Application { get; set; } = new();

    public GeneralSettings General { get; set; } = new();

    public NotificationSettings Notifications { get; set; } = new();

    public AdvancedSettings Advanced { get; set; } = new();
}