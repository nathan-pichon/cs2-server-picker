namespace CS2ServerPicker.Services;

public readonly record struct PingResult(bool Success, long LatencyMs, bool TimedOut);

public interface IPingService
{
    Task<PingResult> PingServerAsync(IEnumerable<string> addresses, CancellationToken ct = default);
    void CancelAll();
}
