using System.IO;
using System.Text.Json;
using CS2ServerPicker.Models;
using Microsoft.Extensions.Logging;

namespace CS2ServerPicker.Services;

public sealed class PresetService : IPresetService
{
    private static readonly string PresetsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CS2ServerPicker");

    private static readonly string PresetsFilePath = Path.Combine(PresetsDirectory, "presets.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<PresetService> _logger;

    public PresetService(ILogger<PresetService> logger)
    {
        _logger = logger;
    }

    public async Task<PresetCollection> LoadPresetsAsync()
    {
        try
        {
            if (!File.Exists(PresetsFilePath))
            {
                await EnsurePresetsFileExistsAsync();
                return [];
            }

            var json = await File.ReadAllTextAsync(PresetsFilePath);
            return JsonSerializer.Deserialize<PresetCollection>(json, JsonOptions) ?? [];
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception,
                "Failed to load presets from {Path}. Returning empty collection.", PresetsFilePath);
            return [];
        }
    }

    public async Task SavePresetsAsync(PresetCollection presets)
    {
        await EnsureDirectoryExistsAsync();
        var json = JsonSerializer.Serialize(presets, JsonOptions);
        await File.WriteAllTextAsync(PresetsFilePath, json);
    }

    public async Task AddPresetAsync(string name, bool clustered, List<string> servers)
    {
        var presets = await LoadPresetsAsync();
        var key = name.Replace(" ", "");

        if (presets.ContainsKey(key))
            throw new InvalidOperationException($"Preset '{name}' already exists.");

        presets[key] = new Preset
        {
            PresetName = name,
            Clustered = clustered,
            Servers = servers
        };

        await SavePresetsAsync(presets);
    }

    public async Task UpdatePresetAsync(string originalKey, string name, bool clustered, List<string> servers)
    {
        var presets = await LoadPresetsAsync();

        if (presets.ContainsKey(originalKey))
            presets.Remove(originalKey);

        var newKey = name.Replace(" ", "");
        presets[newKey] = new Preset
        {
            PresetName = name,
            Clustered = clustered,
            Servers = servers
        };

        await SavePresetsAsync(presets);
    }

    public async Task DeletePresetAsync(string key)
    {
        var presets = await LoadPresetsAsync();

        if (presets.Remove(key))
            await SavePresetsAsync(presets);
    }

    public async Task<PresetCollection> ImportPresetsAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var importedPresets = JsonSerializer.Deserialize<PresetCollection>(json, JsonOptions) ?? [];

        var existingPresets = await LoadPresetsAsync();
        foreach (var (key, preset) in importedPresets)
            existingPresets[key] = preset;

        await SavePresetsAsync(existingPresets);
        return existingPresets;
    }

    public async Task ExportPresetsAsync(PresetCollection presets, string filePath)
    {
        var json = JsonSerializer.Serialize(presets, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task EnsurePresetsFileExistsAsync()
    {
        await EnsureDirectoryExistsAsync();
        if (!File.Exists(PresetsFilePath))
            await File.WriteAllTextAsync(PresetsFilePath, "{}");
    }

    private Task EnsureDirectoryExistsAsync()
    {
        Directory.CreateDirectory(PresetsDirectory);
        return Task.CompletedTask;
    }
}
