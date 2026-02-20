using CS2ServerPicker.Models;

namespace CS2ServerPicker.Configuration;

/// <summary>
/// Defines how CS2 server relays are grouped into named clusters.
/// Each cluster merges multiple individual server relays into a single logical entry.
/// Match patterns are checked with <c>Contains</c> (case-insensitive) against each
/// server's display name (e.g., "Frankfurt (Germany) (fra)").
/// </summary>
public static class ClusterConfiguration
{
    /// <summary>
    /// The list of cluster groupings applied when clustered view is enabled.
    /// Order matters: each server is assigned to the FIRST cluster whose patterns match.
    /// </summary>
    public static readonly ClusterDefinition[] Definitions =
    [
        // ─── Europe ──────────────────────────────────────────────────────────────────

        // EU West: Paris, London, Frankfurt, Amsterdam (+Multiplay), Madrid
        new() {
            ClusterName    = "EU West",
            MatchPatterns  = ["Paris", "London", "Frankfurt", "Amsterdam", "Madrid"],
            Region         = ServerRegions.Europe,
            FlagName       = "Frankfurt (Germany) (fra)"
        },

        // EU East: Stockholm (+Bromma/Kista variants), Warsaw, Helsinki, Vienna
        new() {
            ClusterName    = "EU East",
            MatchPatterns  = ["Stockholm", "Warsaw", "Helsinki", "Vienna"],
            Region         = ServerRegions.Europe,
            FlagName       = "Warsaw (Poland) (waw)"
        },

        // ─── North America ───────────────────────────────────────────────────────────

        // NA East: Atlanta, Chicago, New York, Sterling (Virginia)
        new() {
            ClusterName    = "NA East",
            MatchPatterns  = ["Atlanta", "Chicago", "New York", "Sterling"],
            Region         = ServerRegions.NorthAmerica,
            FlagName       = "New York (jfk)"
        },

        // NA West: Los Angeles, Seattle, Dallas (+Multiplay Dallas)
        new() {
            ClusterName    = "NA West",
            MatchPatterns  = ["Los Angeles", "Seattle", "Dallas"],
            Region         = ServerRegions.NorthAmerica,
            FlagName       = "Los Angeles (California) (lax)"
        },

        // ─── South America ───────────────────────────────────────────────────────────

        // SA: Buenos Aires, Lima, Santiago, Sao Paulo
        new() {
            ClusterName    = "South America",
            MatchPatterns  = ["Buenos Aires", "Lima", "Santiago", "Sao Paulo", "São Paulo"],
            Region         = ServerRegions.SouthAmerica,
            FlagName       = "Sao Paulo (Brazil) (gru)"
        },

        // ─── Asia ────────────────────────────────────────────────────────────────────

        // China: ISP-specific relay networks (Perfect World, Alibaba, Tencent)
        // Note: "Hong Kong" is intentionally excluded — Valve's HK relay goes to East Asia.
        new() {
            ClusterName    = "China",
            MatchPatterns  = ["Perfect", "Alibaba", "Tencent"],
            Region         = ServerRegions.Asia,
            FlagName       = "China"
        },

        // East Asia: Valve/Multiplay relays in Hong Kong, Singapore, South Korea
        new() {
            ClusterName    = "East Asia",
            MatchPatterns  = ["Hong Kong", "Singapore", "Seoul"],
            Region         = ServerRegions.Asia,
            FlagName       = "Singapore (sgp)"
        },

        // Japan
        new() {
            ClusterName    = "Japan",
            MatchPatterns  = ["Tokyo"],
            Region         = ServerRegions.Asia,
            FlagName       = "Japan"
        },

        // India
        new() {
            ClusterName    = "India",
            MatchPatterns  = ["Chennai", "Mumbai"],
            Region         = ServerRegions.Asia,
            FlagName       = "India"
        },
    ];
}
