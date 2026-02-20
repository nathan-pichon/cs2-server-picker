namespace CS2ServerPicker.Models;

/// <summary>
/// Identifies which column is used as the primary sort key for the server list.
/// </summary>
public enum SortColumn
{
    Name,
    Region,
    Latency,
    Blocked,
    Favorite,
    Selected
}
