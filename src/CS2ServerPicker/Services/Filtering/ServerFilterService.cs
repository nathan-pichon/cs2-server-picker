using CS2ServerPicker.ViewModels;

namespace CS2ServerPicker.Services.Filtering;

public sealed class ServerFilterService : IServerFilterService
{
    public IEnumerable<ServerItemViewModel> Apply(
        IEnumerable<ServerItemViewModel> servers,
        string searchText,
        string selectedRegion,
        string filterMode)
    {
        var filteredServers = servers;

        filteredServers = ApplySearchTextFilter(filteredServers, searchText);
        filteredServers = ApplyRegionFilter(filteredServers, selectedRegion);
        filteredServers = ApplyViewModeFilter(filteredServers, filterMode);

        return filteredServers;
    }

    private static IEnumerable<ServerItemViewModel> ApplySearchTextFilter(
        IEnumerable<ServerItemViewModel> servers, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return servers;

        return servers.Where(server =>
            server.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            server.Region.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<ServerItemViewModel> ApplyRegionFilter(
        IEnumerable<ServerItemViewModel> servers, string selectedRegion)
    {
        if (selectedRegion == "All")
            return servers;

        return servers.Where(server => server.Region == selectedRegion);
    }

    private static IEnumerable<ServerItemViewModel> ApplyViewModeFilter(
        IEnumerable<ServerItemViewModel> servers, string filterMode)
    {
        return filterMode switch
        {
            "Favorites" => servers.Where(server => server.IsFavorite),
            "Blocked"   => servers.Where(server => server.IsBlocked),
            _           => servers
        };
    }
}
