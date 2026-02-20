namespace CS2ServerPicker.Configuration;

/// <summary>
/// Configuration options for ICMP ping operations used to measure server latency.
/// Bound from the "Ping" section of appsettings.json.
/// </summary>
public sealed class PingServiceOptions
{
    /// <summary>
    /// Maximum time in milliseconds to wait for a ping reply before considering the server unreachable.
    /// </summary>
    public int TimeoutMs { get; set; } = 3000;
}
