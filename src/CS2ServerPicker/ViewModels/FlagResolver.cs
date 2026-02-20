namespace CS2ServerPicker.ViewModels;

/// <summary>
/// Resolves the WPF pack URI for a server's flag image asset.
/// </summary>
internal static class FlagResolver
{
    /// <summary>
    /// Returns the pack URI pointing to the flag image for the given server name.
    /// </summary>
    public static string GetFlagPackUri(string serverName)
    {
        var sanitizedName = serverName.Replace("/", "").Replace("\\", "");
        return $"pack://application:,,,/Assets/Flags/{sanitizedName}.png";
    }
}
