namespace IconSwapperGui.Core.Models;

public partial class Notification
{
    public void RefreshTimeBindings()
    {
        OnPropertyChanged(nameof(Timestamp));
    }
}