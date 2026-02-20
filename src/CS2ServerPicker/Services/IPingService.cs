namespace CS2ServerPicker.Services;

/// <summary>
/// Result of a single ping operation against a server's relay addresses.
/// </summary>
public readonly record struct PingResult(bool Success, long LatencyMs, bool TimedOut);

/// <summary>
/// Measures round-trip latency to CS2 relay servers using UDP probes.
/// </summary>
public interface IPingService
{
    /// <summary>
    /// Pings all <paramref name="addresses"/> for a server and returns the best result.
    /// Returns a timed-out result if no address responds within the configured timeout.
    /// </summary>
    /// <param name="addresses">One or more relay IP addresses belonging to the same server.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<PingResult> PingServerAsync(IEnumerable<string> addresses, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels all in-progress ping operations immediately.
    /// </summary>
    void CancelAll();
}
