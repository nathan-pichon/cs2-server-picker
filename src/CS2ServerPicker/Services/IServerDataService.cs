using CS2ServerPicker.Models;

namespace CS2ServerPicker.Services;

/// <summary>
/// Fetches and parses CS2 relay server data from the Steam SDR Config API.
/// </summary>
public interface IServerDataService
{
    /// <summary>
    /// Downloads the latest SDR config from Steam and returns the revision string
    /// together with the parsed list of relay servers.
    /// Returns <c>null</c> if the request fails or the response is malformed.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the HTTP request.</param>
    Task<(string revision, List<ServerInfo> servers)?> FetchServerDataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the server list from the most recent successful <see cref="FetchServerDataAsync"/> call.
    /// Returns an empty list if no data has been fetched yet.
    /// </summary>
    List<ServerInfo> GetAllServers();

    /// <summary>
    /// Groups the given servers into regional clusters as defined by <c>ClusterConfiguration</c>.
    /// Servers that do not belong to any cluster are kept as individual entries.
    /// </summary>
    /// <param name="servers">The flat server list to cluster.</param>
    Dictionary<string, ClusteredServer> BuildClusteredView(List<ServerInfo> servers);
}
