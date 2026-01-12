using IconSwapperGui.Core.Models.Swapper.IconVersionManagement;

namespace IconSwapperGui.Core.Interfaces;

public interface IIconHistoryService
{
    Task<IconVersion> RecordIconChangeAsync(string filePath, string iconPath);

    Task<IconHistory> GetHistoryAsync(string filePath);

    Task<IEnumerable<IconHistory>> GetAllHistoriesAsync();

    Task<bool> RevertToVersionAsync(string filePath, Guid versionId);

    Task<bool> DeleteVersionAsync(string filePath, Guid versionId);

    Task<bool> ClearHistoryAsync(string filePath);

    Task<bool> ClearAllHistoryAsync();

    Task<int> GetVersionCountAsync(string filePath);

    Task<bool> HasHistoryAsync(string filePath);
}