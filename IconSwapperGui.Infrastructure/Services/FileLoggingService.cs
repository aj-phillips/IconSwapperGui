using IconSwapperGui.Core.Config;
using IconSwapperGui.Core.Interfaces;

namespace IconSwapperGui.Infrastructure.Services;

public class FileLoggingService : ILoggingService
{
    private readonly object _lockObject = new();
    private readonly string _logFilePath;

    public FileLoggingService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppInfo.ApplicationName,
            "Logs"
        );

        Directory.CreateDirectory(appDataPath);
        _logFilePath = Path.Combine(appDataPath, $"app_{DateTime.Now:ddMMyyyy}.log");
    }

    public bool IsEnabled { get; set; }

    public void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    public void LogWarning(string message)
    {
        WriteLog("WARN", message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        var fullMessage = exception != null
            ? $"{message} | Exception: {exception.Message}\nStackTrace: {exception.StackTrace}"
            : message;

        WriteLog("ERROR", fullMessage);
    }

    public void LogDebug(string message)
    {
        WriteLog("DEBUG", message);
    }

    public Task<string> GetLogFilePathAsync()
    {
        return Task.FromResult(_logFilePath);
    }

    public async Task<List<string>> GetRecentLogsAsync(int count = 100)
    {
        try
        {
            if (!File.Exists(_logFilePath))
                return new List<string>();

            var lines = await File.ReadAllLinesAsync(_logFilePath);
            return lines.TakeLast(count).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private void WriteLog(string level, string message)
    {
        if (!IsEnabled) return;

        try
        {
            lock (_lockObject)
            {
                var logEntry = $"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] [{level}] {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
        }
        catch
        {
            // Fail silently for logging errors
        }
    }
}