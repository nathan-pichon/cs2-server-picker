using System.Net.NetworkInformation;

namespace CS2ServerPicker.Services;

public sealed class PingService : IPingService
{
    private readonly List<Ping> _activePings = [];
    private readonly Lock _lock = new();

    public async Task<PingResult> PingServerAsync(IEnumerable<string> addresses, CancellationToken ct = default)
    {
        var ping = new Ping();

        lock (_lock)
        {
            _activePings.Add(ping);
        }

        try
        {
            foreach (var address in addresses)
            {
                if (ct.IsCancellationRequested)
                    return new PingResult(false, 0, false);

                try
                {
                    var reply = await ping.SendPingAsync(address, 3000);
                    if (reply.Status == IPStatus.Success && reply.RoundtripTime > 0)
                    {
                        return new PingResult(true, reply.RoundtripTime, false);
                    }
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
            lock (_lock)
            {
                _activePings.Remove(ping);
            }

            ping.Dispose();
        }
    }

    public void CancelAll()
    {
        lock (_lock)
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
                    // Ignore disposal errors
                }
            }

            _activePings.Clear();
        }
    }
}
