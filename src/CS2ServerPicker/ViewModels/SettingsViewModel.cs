using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2ServerPicker.Models;
using CS2ServerPicker.Services;
using Wpf.Ui.Appearance;

namespace CS2ServerPicker.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IFirewallService _firewallService;
    private readonly IUpdateService _updateService;
    private readonly AppSettings _settings;

    public SettingsViewModel(
        IFirewallService firewallService,
        IUpdateService updateService,
        AppSettings settings)
    {
        _firewallService = firewallService;
        _updateService = updateService;
        _settings = settings;
        _checkForUpdates = settings.CheckForUpdatesOnStartup;
        _selectedTheme = settings.Theme;
        _autoRefreshInterval = settings.AutoRefreshIntervalSeconds;
    }

    [ObservableProperty]
    private bool _checkForUpdates;

    [ObservableProperty]
    private string _selectedTheme;

    [ObservableProperty]
    private int _autoRefreshInterval;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private string _firewallStatus = "";

    [ObservableProperty]
    private string _currentVersion = "3.0.0";

    public string[] ThemeOptions { get; } = ["Dark", "Light"];

    public int[] RefreshIntervals { get; } = [0, 30, 60, 120, 300];

    partial void OnCheckForUpdatesChanged(bool value)
    {
        _settings.CheckForUpdatesOnStartup = value;
        _settings.Save();
    }

    partial void OnSelectedThemeChanged(string value)
    {
        _settings.Theme = value;
        _settings.Save();
        ApplyTheme(value);
    }

    partial void OnAutoRefreshIntervalChanged(int value)
    {
        _settings.AutoRefreshIntervalSeconds = value;
        _settings.Save();
    }

    [RelayCommand]
    private async Task CheckFirewallAsync()
    {
        var enabled = await _firewallService.CheckFirewallEnabledAsync();
        FirewallStatus = enabled ? "Windows Firewall is enabled." : "Windows Firewall is disabled!";
    }

    [RelayCommand]
    private async Task ResetFirewallAsync()
    {
        await _firewallService.ResetFirewallAsync();
        StatusText = "Firewall has been reset to defaults.";
    }

    [RelayCommand]
    private async Task CheckForUpdateAsync()
    {
        StatusText = "Checking for updates...";
        var update = await _updateService.CheckForUpdateAsync();

        if (update is null)
        {
            StatusText = "Could not check for updates. Check your internet connection.";
            return;
        }

        if (update.UpdateAvailable)
        {
            StatusText = $"Update available: v{update.LatestVersion} (current: v{update.CurrentVersion})";
        }
        else
        {
            StatusText = "You are running the latest version.";
        }
    }

    public static void ApplyTheme(string theme)
    {
        var appTheme = theme switch
        {
            "Light" => ApplicationTheme.Light,
            _ => ApplicationTheme.Dark,
        };
        ApplicationThemeManager.Apply(appTheme);
    }
}
