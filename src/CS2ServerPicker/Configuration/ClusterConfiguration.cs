using CS2ServerPicker.Models;

namespace CS2ServerPicker.Configuration;

/// <summary>
/// Defines how CS2 server relays are grouped into named clusters.
/// Each cluster merges multiple individual server relays into a single logical entry
/// (e.g., all China-region relays under a single "China" entry).
/// </summary>
public static class ClusterConfiguration
{
    /// <summary>
    /// The list of cluster groupings applied when clustered view is enabled.
    /// Servers whose names match any pattern in <see cref="ClusterDefinition.MatchPatterns"/>
    /// are merged into the corresponding cluster.
    /// </summary>
    public static readonly ClusterDefinition[] Definitions =
    [
        new() { ClusterName = "China",             MatchPatterns = ["Perfect", "Hong Kong", "Alibaba", "Tencent"], Region = ServerRegions.Asia },
        new() { ClusterName = "Japan",             MatchPatterns = ["Tokyo"],                                      Region = ServerRegions.Asia },
        new() { ClusterName = "Stockholm (Sweden)", MatchPatterns = ["Stockholm"],                                 Region = ServerRegions.Europe, FlagName = "Stockholm - Bromma (Sweden) (sto2)" },
        new() { ClusterName = "India",             MatchPatterns = ["Chennai", "Mumbai"],                          Region = ServerRegions.Asia },
    ];
}
