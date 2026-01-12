namespace IconSwapperGui.Core.Models.Settings;

public class NotificationSettings
{
    public bool PlaySound { get; set; } = false;
    public bool PlaySoundInfo { get; set; } = true;
    public bool PlaySoundSuccess { get; set; } = true;
    public bool PlaySoundWarning { get; set; } = true;
    public bool PlaySoundError { get; set; } = true;
}