using System.Net.Http;
using System.Windows;
using CS2ServerPicker.Configuration;
using CS2ServerPicker.Models;
using CS2ServerPicker.Services;
using CS2ServerPicker.Services.Filtering;
using CS2ServerPicker.Services.Settings;
using CS2ServerPicker.Services.Sorting;
using CS2ServerPicker.ViewModels;
using CS2ServerPicker.Views;
using CS2ServerPicker.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wpf.Ui;

namespace CS2ServerPicker;

public partial class App : Application
{
    private readonly IHost _host;
    private ILogger<App>? _logger;

    public App()
    {
        try
        {
            _host = Host.CreateDefaultBuilder()
                // AppContext.BaseDirectory is always the exe directory, regardless of the
                // current working directory. This matters because the app requires admin
                // elevation (app.manifest), and UAC can change the CWD to a system path,
                // causing appsettings.json to not be found by the default content root.
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    // External service configuration (URLs, timeouts) — from appsettings.json
                    services.Configure<SteamApiOptions>(context.Configuration.GetSection("SteamApi"));
                    services.Configure<GitHubOptions>(context.Configuration.GetSection("GitHub"));
                    services.Configure<PingServiceOptions>(context.Configuration.GetSection("Ping"));

                    // Settings persistence (repository loads settings at DI resolution time)
                    services.AddSingleton<ISettingsRepository, SettingsRepository>();
                    services.AddSingleton<AppSettings>(sp =>
                        sp.GetRequiredService<ISettingsRepository>().Load());

                    // HTTP client
                    services.AddSingleton<HttpClient>();

                    // Core services (stateful singletons)
                    services.AddSingleton<IServerDataService, ServerDataService>();
                    services.AddSingleton<IFirewallService, FirewallService>();
                    services.AddSingleton<IPingService, PingService>();
                    services.AddSingleton<IPresetService, PresetService>();
                    services.AddSingleton<IUpdateService, UpdateService>();

                    // Stateless services (transient — no shared state, pure logic)
                    services.AddTransient<IServerFilterService, ServerFilterService>();
                    services.AddTransient<IServerSortService, ServerSortService>();

                    // Navigation
                    services.AddSingleton<INavigationService, NavigationService>();

                    // ViewModels
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<ServersPageViewModel>();
                    services.AddSingleton<PresetsPageViewModel>();
                    services.AddSingleton<SettingsViewModel>();

                    // Views
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<ServersPage>();
                    services.AddTransient<PresetsPage>();
                    services.AddTransient<SettingsPage>();
                })
                .Build();

            RegisterGlobalExceptionHandlers();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.ToString(),
                "Startup error (host creation)",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            throw;
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            await _host.StartAsync();

            _logger = _host.Services.GetRequiredService<ILogger<App>>();
            _logger.LogInformation("Host started successfully.");

            var settings = _host.Services.GetRequiredService<AppSettings>();
            SettingsViewModel.ApplyTheme(settings.Theme);
            _logger.LogInformation("Theme applied: {Theme}.", settings.Theme);

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            await CheckForUpdatesIfEnabledAsync(settings);

            base.OnStartup(e);
        }
        catch (Exception exception)
        {
            _logger?.LogCritical(exception, "Fatal error during startup.");
            MessageBox.Show(
                exception.ToString(),
                "Startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var serversViewModel = _host.Services.GetRequiredService<ServersPageViewModel>();
        serversViewModel.Cleanup();

        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            _logger?.LogError(args.Exception, "Unhandled UI exception.");
            MessageBox.Show(
                args.Exception.ToString(),
                "Unhandled UI exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
                _logger?.LogCritical(exception, "Unhandled domain exception.");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger?.LogError(args.Exception, "Unobserved task exception.");
        };
    }

    private async Task CheckForUpdatesIfEnabledAsync(AppSettings settings)
    {
        if (!settings.CheckForUpdatesOnStartup) return;

        var updateService = _host.Services.GetRequiredService<IUpdateService>();
        var updateInfo = await updateService.CheckForUpdateAsync();

        if (updateInfo is not { UpdateAvailable: true }) return;

        var userChoice = MessageBox.Show(
            $"A new version (v{updateInfo.LatestVersion}) is available. Open releases page?",
            "Update Available",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (userChoice == MessageBoxResult.Yes)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = updateInfo.ReleaseUrl,
                UseShellExecute = true
            });
        }
    }
}
