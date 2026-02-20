namespace CS2ServerPicker.Configuration;

/// <summary>
/// Configuration options for the Steam Web API used to fetch CS2 server relay data.
/// Bound from the "SteamApi" section of appsettings.json.
/// </summary>
public sealed class SteamApiOptions
{
    /// <summary>
    /// The full URL of the Steam SDR config endpoint that returns CS2 relay server data.
    /// </summary>
    public string EndpointUrl { get; set; } = string.Empty;
}
