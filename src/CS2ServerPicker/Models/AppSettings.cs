using System.IO;
using System.Text.Json;

namespace CS2ServerPicker.Models;

public sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CS2ServerPicker",
        "settings.json");

    public string ServerRevision { get; set; } = string.Empty;
    public bool IsClustered { get; set; } = true;
    public bool CheckForUpdatesOnStartup { get; set; } = true;
    public int AutoRefreshIntervalSeconds { get; set; } = 0; // 0 = disabled
    public string Theme { get; set; } = "Dark"; // Dark, Light, System
    public HashSet<string> FavoriteServers { get; set; } = [];

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Return defaults on any error
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail on save errors
        }
    }
}
