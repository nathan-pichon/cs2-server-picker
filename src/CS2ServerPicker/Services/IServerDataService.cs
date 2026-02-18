using CS2ServerPicker.Models;

namespace CS2ServerPicker.Services;

public interface IServerDataService
{
    Task<(string revision, List<ServerInfo> servers)?> FetchServerDataAsync(CancellationToken ct = default);
    List<ServerInfo> GetAllServers();
    Dictionary<string, ClusteredServer> BuildClusteredView(List<ServerInfo> servers);
}
