namespace CS2ServerPicker.Services;

/// <summary>
/// Manages Windows Firewall rules to block or allow CS2 relay servers.
/// All operations are idempotent — calling them when the rule already exists
/// (or already doesn't exist) is safe.
/// </summary>
public interface IFirewallService
{
    /// <summary>
    /// Creates an outbound block rule for the given relay server.
    /// Returns <c>true</c> if the rule was applied successfully.
    /// </summary>
    /// <param name="ruleName">The unique firewall rule name for this server.</param>
    /// <param name="remoteIps">Comma-separated list of IP addresses to block.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<bool> BlockServerAsync(string ruleName, string remoteIps, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the outbound block rule for the given relay server.
    /// Returns <c>true</c> if the rule was removed successfully.
    /// </summary>
    /// <param name="ruleName">The unique firewall rule name to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<bool> UnblockServerAsync(string ruleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> if an outbound block rule currently exists for the given server.
    /// </summary>
    /// <param name="ruleName">The firewall rule name to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<bool> IsServerBlockedAsync(string ruleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets Windows Firewall to its default policy, removing all custom rules.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ResetFirewallAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> if the Windows Firewall service is currently enabled.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<bool> CheckFirewallEnabledAsync(CancellationToken cancellationToken = default);
}
