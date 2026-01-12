using IconSwapperGui.Core.Models.Swapper;

namespace IconSwapperGui.Core.Interfaces;

public interface IShortcutService
{
    IEnumerable<Shortcut> GetShortcuts(string? folderPath);
}