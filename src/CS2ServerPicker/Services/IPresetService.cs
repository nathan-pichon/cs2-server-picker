using CS2ServerPicker.Models;

namespace CS2ServerPicker.Services;

/// <summary>
/// Manages the user's named server presets: loading, saving, and CRUD operations.
/// Presets are persisted to a JSON file in the user's local app data folder.
/// </summary>
public interface IPresetService
{
    /// <summary>
    /// Loads all saved presets from disk.
    /// Returns an empty collection if the file does not exist or cannot be read.
    /// </summary>
    Task<PresetCollection> LoadPresetsAsync();

    /// <summary>
    /// Overwrites the persisted preset file with the provided collection.
    /// </summary>
    Task SavePresetsAsync(PresetCollection presets);

    /// <summary>
    /// Adds a new preset with the given name and server list.
    /// Throws <see cref="InvalidOperationException"/> if a preset with the same key already exists.
    /// </summary>
    /// <param name="name">Human-readable preset name.</param>
    /// <param name="clustered">Whether the preset targets clustered server groups.</param>
    /// <param name="servers">Server names to include in the preset.</param>
    Task AddPresetAsync(string name, bool clustered, List<string> servers);

    /// <summary>
    /// Updates an existing preset, replacing its name, cluster mode, and server list.
    /// If the name changed, the old key is removed and the new key is created.
    /// </summary>
    /// <param name="originalKey">The storage key of the preset to update.</param>
    /// <param name="name">New human-readable name.</param>
    /// <param name="clustered">New cluster mode value.</param>
    /// <param name="servers">New server list.</param>
    Task UpdatePresetAsync(string originalKey, string name, bool clustered, List<string> servers);

    /// <summary>
    /// Deletes the preset identified by <paramref name="key"/>.
    /// Does nothing if the key does not exist.
    /// </summary>
    Task DeletePresetAsync(string key);

    /// <summary>
    /// Reads presets from <paramref name="filePath"/>, merges them with existing presets
    /// (imported keys overwrite local ones), and persists the result.
    /// Returns the merged collection.
    /// </summary>
    Task<PresetCollection> ImportPresetsAsync(string filePath);

    /// <summary>
    /// Writes <paramref name="presets"/> to <paramref name="filePath"/> as JSON.
    /// </summary>
    Task ExportPresetsAsync(PresetCollection presets, string filePath);
}
