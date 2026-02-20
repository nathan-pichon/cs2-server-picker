namespace CS2ServerPicker.ViewModels;

/// <summary>
/// Resolves the WPF pack URI for a server's flag image asset.
/// </summary>
internal static class FlagResolver
{
    /// <summary>
    /// Returns the pack URI pointing to the flag image for the given server name.
    /// </summary>
    /// <param name="serverName">The display name of the server or cluster.</param>
    /// <param name="flagNameOverride">
    /// Optional override for the flag asset file name (without extension).
    /// Use when the server/cluster name doesn't match the flag asset name.
    /// </param>
    public static string GetFlagPackUri(string serverName, string? flagNameOverride = null)
    {
        var assetName = flagNameOverride ?? serverName;
        var sanitizedName = assetName.Replace("/", "").Replace("\\", "");
        return $"pack://application:,,,/Assets/Flags/{sanitizedName}.png";
    }
}
