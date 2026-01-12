using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using IconSwapperGui.Core.Interfaces;
using IconSwapperGui.Infrastructure.Services;
using IconSwapperGui.UI.Services;
using IconSwapperGui.UI.Services.Converter;
using IconSwapperGui.UI.Services.Swapper;
using IconSwapperGui.UI.ViewModels;
using IconSwapperGui.UI.Views;
using IconSwapperGui.UI.Windows;
using Velopack;

namespace IconSwapperGui.UI;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public IServiceProvider Services => _serviceProvider;

    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build().Run();
        
        App app = new();
        app.InitializeComponent();
        app.Run();
    }

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDispatcher, EventDispatcherService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ThemeApplier>();
        services.AddSingleton<ILoggingService, FileLoggingService>();
        services.AddSingleton<IUpdateService, VelopackUpdateService>();
        services.AddSingleton<IElevationService, ElevationService>();
        services.AddSingleton<IStartupService, StartupService>();
        services.AddSingleton<IFileSystemWatcherService, FileSystemWatcherService>();
        services.AddSingleton<IFolderManagementService, FolderManagementService>();
        services.AddSingleton<IIconHistoryService, IconHistoryService>();
        services.AddSingleton<IIconManagementService, IconManagementService>();
        services.AddSingleton<IShortcutService, ShortcutService>();
        services.AddSingleton<ILnkSwapperService, LnkSwapperService>();
        services.AddSingleton<IUrlSwapperService, UrlSwapperService>();
        services.AddSingleton<IIconCreatorService, IconCreatorService>();

        services.AddSingleton<UI.Services.PixelArtEditor.PixelArtRenderService>();
        services.AddSingleton<UI.Services.PixelArtEditor.PixelArtExportService>();

        // Views
        services.AddTransient<SwapperView>();
        services.AddTransient<ConverterView>();
        services.AddTransient<PixelArtEditorView>();
        services.AddTransient<SettingsView>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<NotificationPanelViewModel>();
        services.AddTransient<SwapperViewModel>();
        services.AddTransient<ConverterViewModel>();
        services.AddTransient<PixelArtEditorViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<IconVersionManagerViewModel>();
        services.AddTransient<Func<string, IconVersionManagerViewModel>>(sp =>
            filePath => ActivatorUtilities.CreateInstance<IconVersionManagerViewModel>(sp, filePath));

        // Windows
        services.AddSingleton<MainWindow>();
        services.AddSingleton<IconVersionManagerWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var loggingService = _serviceProvider.GetRequiredService<ILoggingService>();
        var updateService = _serviceProvider.GetRequiredService<IUpdateService>();
        var notificationService = _serviceProvider.GetRequiredService<INotificationService>();
        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        var startupService = _serviceProvider.GetRequiredService<IStartupService>();
        _serviceProvider.GetRequiredService<ThemeApplier>();

        await settingsService.LoadSettingsAsync();

        loggingService.IsEnabled = settingsService.Settings.Advanced.EnableLogging;

        loggingService.LogInfo("Application started.");

        var isStartupEnabled = startupService.IsStartupEnabled();
        if (settingsService.Settings.General.LaunchAtStartup != isStartupEnabled)
        {
            if (settingsService.Settings.General.LaunchAtStartup)
            {
                startupService.EnableStartup();
            }
            else
            {
                startupService.DisableStartup();
            }
        }

        var main = _serviceProvider.GetRequiredService<MainWindow>();
        main.Show();

        themeService.ApplyTheme(settingsService.Settings.Appearance.Theme);

        if (settingsService.Settings.General.CheckForUpdates)
            await Task.Run(async () =>
            {
                try
                {
                    loggingService.LogInfo("Checking for updates on startup");
                    var updateInfo = await updateService.CheckForUpdatesAsync();

                    if (updateInfo != null)
                    {
                        Current?.Dispatcher.InvokeAsync(() =>
                        {
                            notificationService.AddNotification(
                                "Update Available",
                                $"Version {updateInfo.Version} is available. Check Settings to update."
                            );
                        });

                        loggingService.LogInfo($"Update available: {updateInfo.Version}");
                    }
                    else
                    {
                        loggingService.LogInfo("No updates available");
                    }
                }
                catch (Exception ex)
                {
                    loggingService.LogError("Failed to check for updates on startup", ex);
                }
            });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();

        base.OnExit(e);
    }
}