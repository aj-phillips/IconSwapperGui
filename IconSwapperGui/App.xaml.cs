using System.IO;
using System.Windows;
using Serilog;

namespace IconSwapperGui;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string logDirectory = Path.Combine(exeDirectory, "logs");

        Directory.CreateDirectory(logDirectory);

        string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string logFileName = $"iconswapper-{timestamp}.log";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: Path.Combine(logDirectory, logFileName),
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30)
            .CreateLogger();

        Log.Information("Application starting up - Log file: {LogFileName}", logFileName);

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            Log.Fatal(ex.ExceptionObject as Exception, "Unhandled exception");

        DispatcherUnhandledException += (s, ex) =>
        {
            Log.Fatal(ex.Exception, "Unhandled dispatcher exception");
            ex.Handled = true;
        };

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}