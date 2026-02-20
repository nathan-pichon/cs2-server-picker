using System.Net.NetworkInformation;
using CS2ServerPicker.Configuration;
using Microsoft.Extensions.Options;

namespace CS2ServerPicker.Services;

public sealed class PingService : IPingService
{
    private readonly List<Ping> _activePings = [];
    private readonly object _activePingsLock = new();
    private readonly int _pingTimeoutMs;

    public PingService(IOptions<PingServiceOptions> pingOptions)
    {
        _pingTimeoutMs = pingOptions.Value.TimeoutMs;
    }

    public async Task<PingResult> PingServerAsync(IEnumerable<string> addresses, CancellationToken cancellationToken = default)
    {
        var ping = new Ping();

        lock (_activePingsLock)
        {
            _activePings.Add(ping);
        }

        try
        {
            foreach (var address in addresses)
            {
                if (cancellationToken.IsCancellationRequested)
                    return new PingResult(false, 0, false);

                try
                {
                    var reply = await ping.SendPingAsync(address, _pingTimeoutMs);
                    if (reply.Status == IPStatus.Success && reply.RoundtripTime > 0)
                        return new PingResult(true, reply.RoundtripTime, false);
                }
                catch (PingException)
                {
                    continue;
                }
                catch (OperationCanceledException)
                {
                    return new PingResult(false, 0, false);
                }
            }

            return new PingResult(false, 0, true);
        }
        finally
        {
            lock (_activePingsLock)
            {
                _activePings.Remove(ping);
            }

            ping.Dispose();
        }
    }

    public void CancelAll()
    {
        lock (_activePingsLock)
        {
            foreach (var ping in _activePings)
            {
                try
                {
                    ping.SendAsyncCancel();
                    ping.Dispose();
                }
                catch
                {
                    // Ignore errors during cancellation — the ping is being torn down
                }
            }

            _activePings.Clear();
        }
    }
}
