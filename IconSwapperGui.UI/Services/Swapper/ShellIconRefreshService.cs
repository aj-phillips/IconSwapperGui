using System.Runtime.InteropServices;
using IconSwapperGui.Core.Interfaces;

namespace IconSwapperGui.UI.Services.Swapper;

public sealed class ShellIconRefreshService : IShellIconRefreshService
{
    private const uint ShcneUpdateItem = 0x00002000;
    private const uint ShcneAssocChanged = 0x08000000;
    private const uint ShcnfPathW = 0x0005;
    private const uint ShcnfFlushNowait = 0x2000;
    private const uint ShcnfIdList = 0x0000;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, string? dwItem1, string? dwItem2);

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public void RefreshIcon(string path)

    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        SHChangeNotify(ShcneUpdateItem, ShcnfPathW | ShcnfFlushNowait, path, null);
        SHChangeNotify(ShcneAssocChanged, ShcnfIdList | ShcnfFlushNowait, IntPtr.Zero, IntPtr.Zero);
    }
}
