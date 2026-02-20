using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2ServerPicker.Models;
using CS2ServerPicker.Services;
using CS2ServerPicker.Services.Settings;
using Wpf.Ui.Appearance;

namespace CS2ServerPicker.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IFirewallService _firewallService;
    private readonly IUpdateService _updateService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly AppSettings _settings;

    public SettingsViewModel(
        IFirewallService firewallService,
        IUpdateService updateService,
        ISettingsRepository settingsRepository,
        AppSettings settings)
    {
        _firewallService = firewallService;
        _updateService = updateService;
        _settingsRepository = settingsRepository;
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
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string _firewallStatus = string.Empty;

    [ObservableProperty]
    private string _currentVersion = "3.0.0";

    public string[] ThemeOptions { get; } = ["Dark", "Light"];

    public int[] RefreshIntervals { get; } = [0, 30, 60, 120, 300];

    partial void OnCheckForUpdatesChanged(bool value)
    {
        _settings.CheckForUpdatesOnStartup = value;
        _settingsRepository.Save(_settings);
    }

    partial void OnSelectedThemeChanged(string value)
    {
        _settings.Theme = value;
        _settingsRepository.Save(_settings);
        ApplyTheme(value);
    }

    partial void OnAutoRefreshIntervalChanged(int value)
    {
        _settings.AutoRefreshIntervalSeconds = value;
        _settingsRepository.Save(_settings);
    }

    [RelayCommand]
    private async Task CheckFirewallAsync()
    {
        var isEnabled = await _firewallService.CheckFirewallEnabledAsync();
        FirewallStatus = isEnabled ? "Windows Firewall is enabled." : "Windows Firewall is disabled!";
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
        var updateInfo = await _updateService.CheckForUpdateAsync();

        if (updateInfo is null)
        {
            StatusText = "Could not check for updates. Check your internet connection.";
            return;
        }

        StatusText = updateInfo.UpdateAvailable
            ? $"Update available: v{updateInfo.LatestVersion} (current: v{updateInfo.CurrentVersion})"
            : "You are running the latest version.";
    }

    public static void ApplyTheme(string theme)
    {
        var applicationTheme = theme switch
        {
            "Light" => ApplicationTheme.Light,
            _       => ApplicationTheme.Dark,
        };
        ApplicationThemeManager.Apply(applicationTheme);
    }
}
