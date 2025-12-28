using System.IO;
using System.Windows;
using Serilog;

namespace IconSwapperGui;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private static bool _isLoggerConfigured;
    private static readonly object LoggerLock = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        lock (LoggerLock)
        {
            if (!_isLoggerConfigured)
            {
                var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var logDirectory = Path.Combine(exeDirectory, "logs");

                Directory.CreateDirectory(logDirectory);

                try
                {
                    var threshold = DateTime.UtcNow.AddDays(-7);
                    foreach (var file in Directory.EnumerateFiles(logDirectory, "iconswapper-*.log"))
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            if (fi.CreationTimeUtc < threshold)
                                fi.Delete();
                        }
                        catch
                        {
                            // ignore file delete errors
                        }
                    }
                }
                catch
                {
                    // ignore enumerating errors
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
                var logFileName = $"iconswapper-{timestamp}.log";

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .Enrich.FromLogContext()
                    .WriteTo.File(
                        path: Path.Combine(logDirectory, logFileName),
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                        retainedFileCountLimit: 30,
                        shared: true)
                    .CreateLogger();

                Log.Information("Application starting up - Log file: {LogFileName}", logFileName);

                AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                    Log.Fatal(ex.ExceptionObject as Exception, "Unhandled exception");

                DispatcherUnhandledException += (s, ex) =>
                {
                    Log.Fatal(ex.Exception, "Unhandled dispatcher exception");
                    ex.Handled = true;
                };

                _isLoggerConfigured = true;
            }
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}