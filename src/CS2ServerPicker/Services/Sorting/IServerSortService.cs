using CS2ServerPicker.Models;
using CS2ServerPicker.ViewModels;

namespace CS2ServerPicker.Services.Sorting;

/// <summary>
/// Sorts a server collection by a specified column in ascending or descending order.
/// </summary>
public interface IServerSortService
{
    /// <summary>
    /// Returns <paramref name="servers"/> sorted by <paramref name="sortColumn"/>.
    /// </summary>
    /// <param name="servers">The servers to sort.</param>
    /// <param name="sortColumn">The column to sort by.</param>
    /// <param name="ascending">True for ascending order, false for descending.</param>
    IEnumerable<ServerItemViewModel> Apply(
        IEnumerable<ServerItemViewModel> servers,
        SortColumn sortColumn,
        bool ascending);
}
