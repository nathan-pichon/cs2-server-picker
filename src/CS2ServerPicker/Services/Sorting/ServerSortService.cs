using CS2ServerPicker.Models;
using CS2ServerPicker.ViewModels;

namespace CS2ServerPicker.Services.Sorting;

public sealed class ServerSortService : IServerSortService
{
    public IEnumerable<ServerItemViewModel> Apply(
        IEnumerable<ServerItemViewModel> servers,
        SortColumn sortColumn,
        bool ascending)
    {
        return sortColumn switch
        {
            SortColumn.Region   => SortBy(servers, server => server.Region,     ascending, StringComparer.OrdinalIgnoreCase),
            SortColumn.Latency  => SortBy(servers, server => server.LatencyMs,  ascending),
            SortColumn.Blocked  => SortBy(servers, server => server.IsBlocked,  ascending),
            SortColumn.Favorite => SortBy(servers, server => server.IsFavorite, ascending),
            SortColumn.Selected => SortBy(servers, server => server.IsSelected, ascending),
            _                   => SortBy(servers, server => server.Name,       ascending, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static IEnumerable<ServerItemViewModel> SortBy<TKey>(
        IEnumerable<ServerItemViewModel> servers,
        Func<ServerItemViewModel, TKey> keySelector,
        bool ascending,
        IComparer<TKey>? comparer = null)
    {
        return ascending
            ? servers.OrderBy(keySelector, comparer)
            : servers.OrderByDescending(keySelector, comparer);
    }
}
