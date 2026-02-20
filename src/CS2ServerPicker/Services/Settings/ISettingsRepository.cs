using CS2ServerPicker.Models;

namespace CS2ServerPicker.Services.Settings;

/// <summary>
/// Handles loading and persisting <see cref="AppSettings"/> to disk.
/// Separates I/O concerns from the <see cref="AppSettings"/> data model.
/// </summary>
public interface ISettingsRepository
{
    /// <summary>
    /// Loads user settings from disk.
    /// Returns default settings if the file does not exist or cannot be read.
    /// </summary>
    AppSettings Load();

    /// <summary>
    /// Writes the provided settings to disk, overwriting any existing file.
    /// Logs a warning if the write fails; does not throw.
    /// </summary>
    void Save(AppSettings settings);
}
