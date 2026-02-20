namespace CS2ServerPicker.Models;

/// <summary>
/// Holds all user-configurable application settings.
/// This is a plain data class — persistence is handled by <see cref="CS2ServerPicker.Services.Settings.ISettingsRepository"/>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// The Steam SDR config revision string from the last successful data fetch.
    /// Used to detect when Valve has updated the relay server list.
    /// </summary>
    public string ServerRevision { get; set; } = string.Empty;

    /// <summary>
    /// When true, servers are grouped into regional clusters (e.g., all China relays merged into one entry).
    /// </summary>
    public bool IsClustered { get; set; } = true;

    /// <summary>
    /// When true, the application checks for a newer release on startup.
    /// </summary>
    public bool CheckForUpdatesOnStartup { get; set; } = true;

    /// <summary>
    /// Interval in seconds between automatic ping refreshes. 0 means auto-refresh is disabled.
    /// </summary>
    public int AutoRefreshIntervalSeconds { get; set; } = 0;

    /// <summary>
    /// The UI theme to apply: "Dark", "Light", or "System".
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    /// The set of server names the user has marked as favorites.
    /// </summary>
    public HashSet<string> FavoriteServers { get; set; } = [];

    /// <summary>
    /// The latency threshold (ms) at which the ping bar reaches 100% width and maximum red color.
    /// Servers with a ping at or above this value show a fully red, full-width bar.
    /// </summary>
    public int MaxPingBarMs { get; set; } = 120;
}
