using CS2ServerPicker.Models;

namespace CS2ServerPicker.Services;

public interface IPresetService
{
    Task<PresetCollection> LoadPresetsAsync();
    Task SavePresetsAsync(PresetCollection presets);
    Task AddPresetAsync(string name, bool clustered, List<string> servers);
    Task UpdatePresetAsync(string originalKey, string name, bool clustered, List<string> servers);
    Task DeletePresetAsync(string key);
    Task<PresetCollection> ImportPresetsAsync(string filePath);
    Task ExportPresetsAsync(PresetCollection presets, string filePath);
}
