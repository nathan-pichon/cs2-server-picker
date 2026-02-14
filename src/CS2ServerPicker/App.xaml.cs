using System.Net.Http;
using System.Windows;
using CS2ServerPicker.Models;
using CS2ServerPicker.Services;
using CS2ServerPicker.ViewModels;
using CS2ServerPicker.Views;
using CS2ServerPicker.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;

namespace CS2ServerPicker;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // Settings (singleton)
                var settings = AppSettings.Load();
                services.AddSingleton(settings);

                // HTTP client
                services.AddSingleton<HttpClient>();

                // Services
                services.AddSingleton<IServerDataService, ServerDataService>();
                services.AddSingleton<IFirewallService, FirewallService>();
                services.AddSingleton<IPingService, PingService>();
                services.AddSingleton<IPresetService, PresetService>();
                services.AddSingleton<IUpdateService, UpdateService>();

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
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // Apply saved theme
        var settings = _host.Services.GetRequiredService<AppSettings>();
        SettingsViewModel.ApplyTheme(settings.Theme);

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Check for updates on startup
        if (settings.CheckForUpdatesOnStartup)
        {
            var updateService = _host.Services.GetRequiredService<IUpdateService>();
            var update = await updateService.CheckForUpdateAsync();
            if (update is { UpdateAvailable: true })
            {
                var result = MessageBox.Show(
                    $"A new version (v{update.LatestVersion}) is available. Open releases page?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = update.ReleaseUrl,
                        UseShellExecute = true
                    });
                }
            }
        }

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var serversVm = _host.Services.GetRequiredService<ServersPageViewModel>();
        serversVm.Cleanup();

        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
