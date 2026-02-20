using System.IO;
using System.Text.Json;
using CS2ServerPicker.Models;
using Microsoft.Extensions.Logging;

namespace CS2ServerPicker.Services.Settings;

public sealed class SettingsRepository : ISettingsRepository
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CS2ServerPicker",
        "settings.json");

    private readonly ILogger<SettingsRepository> _logger;

    public SettingsRepository(ILogger<SettingsRepository> logger)
    {
        _logger = logger;
    }

    public AppSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Failed to load settings from {Path}. Using defaults.", SettingsFilePath);
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception,
                "Failed to save settings to {Path}.", SettingsFilePath);
        }
    }
}
