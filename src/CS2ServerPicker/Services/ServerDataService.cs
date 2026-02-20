using System.Net.Http;
using System.Text.Json;
using CS2ServerPicker.Configuration;
using CS2ServerPicker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CS2ServerPicker.Services;

public sealed class ServerDataService : IServerDataService
{
    private readonly HttpClient _httpClient;
    private readonly SteamApiOptions _steamApiOptions;
    private readonly ILogger<ServerDataService> _logger;
    private List<ServerInfo> _servers = [];

    public ServerDataService(
        HttpClient httpClient,
        IOptions<SteamApiOptions> steamApiOptions,
        ILogger<ServerDataService> logger)
    {
        _httpClient = httpClient;
        _steamApiOptions = steamApiOptions.Value;
        _logger = logger;
    }

    public async Task<(string revision, List<ServerInfo> servers)?> FetchServerDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(_steamApiOptions.EndpointUrl, cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("revision", out var revisionElement))
                return null;

            var revision = revisionElement.ToString();

            if (!root.TryGetProperty("pops", out var pops))
                return null;

            var servers = ParseServersFromPops(pops);
            _servers = servers;
            return (revision, servers);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to fetch server data from Steam API at {Url}.", _steamApiOptions.EndpointUrl);
            return null;
        }
    }

    public List<ServerInfo> GetAllServers() => _servers;

    public Dictionary<string, ClusteredServer> BuildClusteredView(List<ServerInfo> servers)
    {
        var clustered = new Dictionary<string, ClusteredServer>();
        var serversAlreadyAssignedToCluster = new HashSet<string>();

        AssignServersToClusters(servers, clustered, serversAlreadyAssignedToCluster);
        AddRemainingServersAsIndividualEntries(servers, clustered, serversAlreadyAssignedToCluster);

        return clustered;
    }

    private static List<ServerInfo> ParseServersFromPops(JsonElement pops)
    {
        var servers = new List<ServerInfo>();

        foreach (var pop in pops.EnumerateObject())
        {
            if (!pop.Value.TryGetProperty("relays", out var relays))
                continue;

            var relayAddresses = ExtractRelayAddresses(relays);
            if (relayAddresses.Count == 0)
                continue;

            var description = pop.Value.TryGetProperty("desc", out var descElement)
                ? descElement.GetString() ?? pop.Name
                : pop.Name;

            servers.Add(new ServerInfo
            {
                Name = $"{description} ({pop.Name})",
                Code = pop.Name,
                Description = description,
                RelayAddresses = relayAddresses,
                Region = ServerRegions.GetRegion(pop.Name)
            });
        }

        return servers;
    }

    private static List<string> ExtractRelayAddresses(JsonElement relays)
    {
        var addresses = new List<string>();

        foreach (var relay in relays.EnumerateArray())
        {
            if (relay.TryGetProperty("ipv4", out var ipv4))
                addresses.Add(ipv4.GetString()!);
        }

        return addresses;
    }

    private static void AssignServersToClusters(
        List<ServerInfo> servers,
        Dictionary<string, ClusteredServer> clustered,
        HashSet<string> serversAlreadyAssignedToCluster)
    {
        foreach (var clusterDefinition in ClusterConfiguration.Definitions)
        {
            var matchingServers = servers.Where(server =>
                clusterDefinition.MatchPatterns.Any(pattern =>
                    server.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var allClusterAddresses = new List<string>();
            var memberServerNames = new List<string>();

            foreach (var matchingServer in matchingServers)
            {
                allClusterAddresses.AddRange(matchingServer.RelayAddresses);
                memberServerNames.Add(matchingServer.Name);
                serversAlreadyAssignedToCluster.Add(matchingServer.Name);
            }

            if (allClusterAddresses.Count > 0)
            {
                clustered[clusterDefinition.ClusterName] = new ClusteredServer
                {
                    Name = clusterDefinition.ClusterName,
                    AllAddresses = allClusterAddresses,
                    Region = clusterDefinition.Region,
                    MemberServerNames = memberServerNames
                };
            }
        }
    }

    private static void AddRemainingServersAsIndividualEntries(
        List<ServerInfo> servers,
        Dictionary<string, ClusteredServer> clustered,
        HashSet<string> serversAlreadyAssignedToCluster)
    {
        foreach (var server in servers.Where(server => !serversAlreadyAssignedToCluster.Contains(server.Name)))
        {
            if (clustered.TryGetValue(server.Name, out var existingCluster))
            {
                existingCluster.AllAddresses.AddRange(server.RelayAddresses);
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
    }
}
