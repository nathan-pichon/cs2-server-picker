using System.Net.Http;
using System.Text.Json;
using CS2ServerPicker.Models;

namespace CS2ServerPicker.Services;

public sealed class ServerDataService : IServerDataService
{
    private const string SteamApiUrl = "https://api.steampowered.com/ISteamApps/GetSDRConfig/v1/?appid=730";

    private static readonly ClusterDefinition[] ClusterDefinitions =
    [
        new() { ClusterName = "China", MatchPatterns = ["Perfect", "Hong Kong", "Alibaba", "Tencent"], Region = ServerRegions.Asia },
        new() { ClusterName = "Japan", MatchPatterns = ["Tokyo"], Region = ServerRegions.Asia },
        new() { ClusterName = "Stockholm (Sweden)", MatchPatterns = ["Stockholm"], Region = ServerRegions.Europe },
        new() { ClusterName = "India", MatchPatterns = ["Chennai", "Mumbai"], Region = ServerRegions.Asia },
    ];

    private readonly HttpClient _httpClient;
    private List<ServerInfo> _servers = [];

    public ServerDataService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(string revision, List<ServerInfo> servers)?> FetchServerDataAsync(CancellationToken ct = default)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(SteamApiUrl, ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("revision", out var revisionElement))
                return null;

            var revision = revisionElement.ToString();
            var servers = new List<ServerInfo>();

            if (!root.TryGetProperty("pops", out var pops))
                return null;

            foreach (var pop in pops.EnumerateObject())
            {
                if (!pop.Value.TryGetProperty("relays", out var relays))
                    continue;

                var addresses = new List<string>();
                foreach (var relay in relays.EnumerateArray())
                {
                    if (relay.TryGetProperty("ipv4", out var ipv4))
                        addresses.Add(ipv4.GetString()!);
                }

                if (addresses.Count == 0)
                    continue;

                var desc = pop.Value.TryGetProperty("desc", out var descElement)
                    ? descElement.GetString() ?? pop.Name
                    : pop.Name;

                var serverName = $"{desc} ({pop.Name})";
                var region = ServerRegions.GetRegion(pop.Name);

                servers.Add(new ServerInfo
                {
                    Name = serverName,
                    Code = pop.Name,
                    Description = desc,
                    RelayAddresses = addresses,
                    Region = region
                });
            }

            _servers = servers;
            return (revision, servers);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public List<ServerInfo> GetAllServers() => _servers;

    public Dictionary<string, ClusteredServer> BuildClusteredView(List<ServerInfo> servers)
    {
        var clustered = new Dictionary<string, ClusteredServer>();
        var assignedToCluster = new HashSet<string>();

        // Build clusters
        foreach (var cluster in ClusterDefinitions)
        {
            var matchingServers = servers.Where(s =>
                cluster.MatchPatterns.Any(p => s.Name.Contains(p, StringComparison.OrdinalIgnoreCase)));

            var allAddresses = new List<string>();
            foreach (var server in matchingServers)
            {
                allAddresses.AddRange(server.RelayAddresses);
                assignedToCluster.Add(server.Name);
            }

            if (allAddresses.Count > 0)
            {
                clustered[cluster.ClusterName] = new ClusteredServer
                {
                    Name = cluster.ClusterName,
                    AllAddresses = allAddresses,
                    Region = cluster.Region
                };
            }
        }

        // Add non-clustered servers
        foreach (var server in servers.Where(s => !assignedToCluster.Contains(s.Name)))
        {
            if (clustered.TryGetValue(server.Name, out var existing))
            {
                existing.AllAddresses.AddRange(server.RelayAddresses);
            }
            else
            {
                clustered[server.Name] = new ClusteredServer
                {
                    Name = server.Name,
                    AllAddresses = [.. server.RelayAddresses],
                    Region = server.Region
                };
            }
        }

        return clustered;
    }
}
