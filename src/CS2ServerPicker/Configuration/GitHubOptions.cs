namespace CS2ServerPicker.Configuration;

/// <summary>
/// Configuration options for the GitHub API used to check for application updates.
/// Bound from the "GitHub" section of appsettings.json.
/// </summary>
public sealed class GitHubOptions
{
    /// <summary>
    /// The GitHub API URL that returns the list of releases for this repository.
    /// </summary>
    public string ReleasesApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// The GitHub releases page URL opened in the browser when the user accepts an update.
    /// </summary>
    public string ReleasesPageUrl { get; set; } = string.Empty;

    /// <summary>
    /// The version string sent in the HTTP User-Agent header when calling the GitHub API.
    /// </summary>
    public string UserAgentVersion { get; set; } = "1.0";
}
