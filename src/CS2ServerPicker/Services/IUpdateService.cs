namespace CS2ServerPicker.Services;

/// <summary>
/// Information about an available (or not available) application update.
/// </summary>
/// <param name="LatestVersion">The newest version string found on GitHub (e.g. "4.2.0").</param>
/// <param name="CurrentVersion">The version of the running assembly.</param>
/// <param name="UpdateAvailable">True when <paramref name="LatestVersion"/> is newer than <paramref name="CurrentVersion"/>.</param>
/// <param name="ReleaseUrl">URL of the GitHub releases page to direct the user to.</param>
public record UpdateInfo(string LatestVersion, string CurrentVersion, bool UpdateAvailable, string ReleaseUrl);

/// <summary>
/// Checks the GitHub releases API to determine whether a newer version of the application is available.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Queries the GitHub releases API and compares the latest tag against the running assembly version.
    /// Returns <c>null</c> if the check cannot be completed (e.g. no internet connection).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the HTTP request.</param>
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}
