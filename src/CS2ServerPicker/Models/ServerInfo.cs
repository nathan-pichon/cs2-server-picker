namespace CS2ServerPicker.Models;

/// <summary>
/// Represents a single server relay entry from the Steam SDR config.
/// </summary>
public sealed class ServerInfo
{
    public required string Name { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public required List<string> RelayAddresses { get; init; }
    public string? ClusterName { get; set; }
    public string Region { get; set; } = "Unknown";

    public string CombinedAddresses => string.Join(",", RelayAddresses);

    public string FirewallRuleName => $"CS2ServerPicker_{Name.Replace(" ", "")}";

    public string FlagResourceKey => Name;
}

/// <summary>
/// Defines how servers are clustered together (e.g., all China servers into one "China" entry).
/// </summary>
public sealed class ClusterDefinition
{
    public required string ClusterName { get; init; }
    public required string[] MatchPatterns { get; init; }
    public required string Region { get; init; }

    /// <summary>
    /// Optional override for the flag asset lookup. When null, the ClusterName is used.
    /// Set this when the cluster name doesn't match any flag image file name.
    /// </summary>
    public string? FlagName { get; init; }
}

/// <summary>
/// Represents a clustered server (multiple servers merged into one entry).
/// </summary>
public sealed class ClusteredServer
{
    public required string Name { get; init; }
    public required List<string> AllAddresses { get; init; }
    public required string Region { get; init; }

    /// <summary>
    /// Names of the individual servers that were merged into this cluster.
    /// Used to compute average ping across all member servers.
    /// </summary>
    public List<string> MemberServerNames { get; init; } = [];

    public string CombinedAddresses => string.Join(",", AllAddresses);
    public string FirewallRuleName => $"CS2ServerPicker_{Name.Replace(" ", "")}";
}

/// <summary>
/// Regions for grouping servers.
/// </summary>
public static class ServerRegions
{
    public const string Europe = "Europe";
    public const string NorthAmerica = "North America";
    public const string SouthAmerica = "South America";
    public const string Asia = "Asia";
    public const string MiddleEast = "Middle East";
    public const string Africa = "Africa";
    public const string Oceania = "Oceania";

    public static readonly Dictionary<string, string> CodeToRegion = new()
    {
        ["fra"] = Europe, ["ams"] = Europe, ["lhr"] = Europe, ["par"] = Europe,
        ["sto"] = Europe, ["sto2"] = Europe, ["hel"] = Europe, ["vie"] = Europe,
        ["waw"] = Europe, ["mad"] = Europe, ["ams4"] = Europe,
        ["atl"] = NorthAmerica, ["ord"] = NorthAmerica, ["dfw"] = NorthAmerica,
        ["lax"] = NorthAmerica, ["jfk"] = NorthAmerica, ["sea"] = NorthAmerica,
        ["iad"] = NorthAmerica, ["dfw2"] = NorthAmerica,
        ["eze"] = SouthAmerica, ["lim"] = SouthAmerica, ["scl"] = SouthAmerica,
        ["gru"] = SouthAmerica,
        ["hkg"] = Asia, ["sgp"] = Asia, ["seo"] = Asia, ["tyo"] = Asia,
        ["hkg4"] = Asia, ["bom2"] = Asia, ["maa2"] = Asia,
        ["dxb"] = MiddleEast,
        ["jnb"] = Africa,
        ["syd"] = Oceania,
    };

    public static string GetRegion(string serverCode)
    {
        // Handle China/PW/Alibaba/Tencent codes
        if (serverCode.StartsWith("pw") || serverCode.StartsWith("sh") ||
            serverCode.StartsWith("pek") || serverCode.StartsWith("pvg") ||
            serverCode.StartsWith("can") || serverCode.StartsWith("ctu") ||
            serverCode.StartsWith("tsn") || serverCode.StartsWith("tgd"))
            return Asia;

        return CodeToRegion.GetValueOrDefault(serverCode, "Unknown");
    }
}
