using System.IO;
using System.Text.Json;
using CS2ServerPicker.Models;

namespace CS2ServerPicker.Services;

public sealed class PresetService : IPresetService
{
    private static readonly string PresetsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CS2ServerPicker");

    private static readonly string PresetsPath = Path.Combine(PresetsDir, "presets.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<PresetCollection> LoadPresetsAsync()
    {
        try
        {
            if (!File.Exists(PresetsPath))
            {
                await EnsurePresetsFileAsync();
                return [];
            }

            var json = await File.ReadAllTextAsync(PresetsPath);
            return JsonSerializer.Deserialize<PresetCollection>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task SavePresetsAsync(PresetCollection presets)
    {
        await EnsureDirectoryAsync();
        var json = JsonSerializer.Serialize(presets, JsonOptions);
        await File.WriteAllTextAsync(PresetsPath, json);
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

        // Remove old key if name changed
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
        var imported = JsonSerializer.Deserialize<PresetCollection>(json, JsonOptions) ?? [];

        var existing = await LoadPresetsAsync();
        foreach (var (key, preset) in imported)
        {
            existing[key] = preset;
        }

        await SavePresetsAsync(existing);
        return existing;
    }

    public async Task ExportPresetsAsync(PresetCollection presets, string filePath)
    {
        var json = JsonSerializer.Serialize(presets, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task EnsurePresetsFileAsync()
    {
        await EnsureDirectoryAsync();
        if (!File.Exists(PresetsPath))
            await File.WriteAllTextAsync(PresetsPath, "{}");
    }

    private Task EnsureDirectoryAsync()
    {
        if (!Directory.Exists(PresetsDir))
            Directory.CreateDirectory(PresetsDir);
        return Task.CompletedTask;
    }
}
