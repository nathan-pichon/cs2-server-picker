namespace CS2ServerPicker.Constants;

/// <summary>
/// Application-wide domain constants that are stable across environments.
/// These values are intrinsic to the CS2 server picker domain and are not
/// expected to change without a code update.
/// </summary>
internal static class AppConstants
{
    /// <summary>
    /// Constants related to server region classification.
    /// </summary>
    internal static class Regions
    {
        /// <summary>
        /// The ordered list of regions available in the region filter dropdown.
        /// The first entry ("All") represents the no-filter state.
        /// </summary>
        public static readonly string[] FilterOptions =
        [
            "All",
            "Europe",
            "North America",
            "South America",
            "Asia",
            "Middle East",
            "Africa",
            "Oceania"
        ];
    }
}
