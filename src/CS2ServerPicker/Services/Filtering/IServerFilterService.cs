using CS2ServerPicker.ViewModels;

namespace CS2ServerPicker.Services.Filtering;

/// <summary>
/// Filters a server collection based on search text, geographic region, and view mode.
/// </summary>
public interface IServerFilterService
{
    /// <summary>
    /// Returns the subset of <paramref name="servers"/> that match all three criteria:
    /// <paramref name="searchText"/>, <paramref name="selectedRegion"/>, and <paramref name="filterMode"/>.
    /// </summary>
    /// <param name="servers">The full list of servers to filter.</param>
    /// <param name="searchText">Free-text query matched against server name and region. Empty string matches all.</param>
    /// <param name="selectedRegion">Region name to restrict results to, or "All" to include every region.</param>
    /// <param name="filterMode">View mode: "All", "Favorites", or "Blocked".</param>
    IEnumerable<ServerItemViewModel> Apply(
        IEnumerable<ServerItemViewModel> servers,
        string searchText,
        string selectedRegion,
        string filterMode);
}
