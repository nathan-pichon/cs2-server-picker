using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace CS2ServerPicker.Services;

public sealed class FirewallService : IFirewallService
{
    private static readonly string NetshPath = Path.Combine(Environment.SystemDirectory, "netsh.exe");

    private readonly ILogger<FirewallService> _logger;

    public FirewallService(ILogger<FirewallService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> BlockServerAsync(string ruleName, string remoteIps, CancellationToken cancellationToken = default)
    {
        if (await IsServerBlockedAsync(ruleName, cancellationToken))
            return true;

        var arguments = $"advfirewall firewall add rule name={ruleName} dir=out action=block protocol=ANY remoteip={remoteIps}";
        var (exitCode, _) = await RunNetshAsync(arguments, cancellationToken);

        if (exitCode != 0)
            _logger.LogWarning("netsh block rule returned non-zero exit code {ExitCode} for rule {RuleName}.", exitCode, ruleName);

        return exitCode == 0;
    }

    public async Task<bool> UnblockServerAsync(string ruleName, CancellationToken cancellationToken = default)
    {
        if (!await IsServerBlockedAsync(ruleName, cancellationToken))
            return true;

        var arguments = $"advfirewall firewall delete rule name={ruleName}";
        var (exitCode, _) = await RunNetshAsync(arguments, cancellationToken);

        if (exitCode != 0)
            _logger.LogWarning("netsh delete rule returned non-zero exit code {ExitCode} for rule {RuleName}.", exitCode, ruleName);

        return exitCode == 0;
    }

    public async Task<bool> IsServerBlockedAsync(string ruleName, CancellationToken cancellationToken = default)
    {
        var arguments = $"advfirewall firewall show rule name={ruleName}";
        var (_, output) = await RunNetshAsync(arguments, cancellationToken);
        return output.Contains(ruleName);
    }

    public async Task ResetFirewallAsync(CancellationToken cancellationToken = default)
    {
        await RunNetshAsync("advfirewall reset", cancellationToken);
    }

    public async Task<bool> CheckFirewallEnabledAsync(CancellationToken cancellationToken = default)
    {
        var (_, output) = await RunNetshAsync("advfirewall show currentprofile state", cancellationToken);
        return output.Contains("ON", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(int exitCode, string output)> RunNetshAsync(string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = NetshPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        return await Task.Run(() =>
        {
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return (process.ExitCode, output);
        }, cancellationToken);
    }
}
