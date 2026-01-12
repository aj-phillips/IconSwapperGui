namespace IconSwapperGui.Core.Interfaces;

public interface ILoggingService
{
    bool IsEnabled { get; set; }
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
    void LogDebug(string message);
    Task<string> GetLogFilePathAsync();
    Task<List<string>> GetRecentLogsAsync(int count = 100);
}