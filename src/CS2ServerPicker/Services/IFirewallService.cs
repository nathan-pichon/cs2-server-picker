namespace CS2ServerPicker.Services;

public interface IFirewallService
{
    Task<bool> BlockServerAsync(string ruleName, string remoteIps, CancellationToken ct = default);
    Task<bool> UnblockServerAsync(string ruleName, CancellationToken ct = default);
    Task<bool> IsServerBlockedAsync(string ruleName, CancellationToken ct = default);
    Task ResetFirewallAsync(CancellationToken ct = default);
    Task<bool> CheckFirewallEnabledAsync(CancellationToken ct = default);
}
