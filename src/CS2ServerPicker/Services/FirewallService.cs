using System.Diagnostics;
using System.IO;

namespace CS2ServerPicker.Services;

public sealed class FirewallService : IFirewallService
{
    private static readonly string NetshPath = Path.Combine(Environment.SystemDirectory, "netsh.exe");

    public async Task<bool> BlockServerAsync(string ruleName, string remoteIps, CancellationToken ct = default)
    {
        // Check if already blocked
        if (await IsServerBlockedAsync(ruleName, ct))
            return true;

        var args = $"advfirewall firewall add rule name={ruleName} dir=out action=block protocol=ANY remoteip={remoteIps}";
        var (exitCode, _) = await RunNetshAsync(args, ct);
        return exitCode == 0;
    }

    public async Task<bool> UnblockServerAsync(string ruleName, CancellationToken ct = default)
    {
        // Check if already unblocked
        if (!await IsServerBlockedAsync(ruleName, ct))
            return true;

        var args = $"advfirewall firewall delete rule name={ruleName}";
        var (exitCode, _) = await RunNetshAsync(args, ct);
        return exitCode == 0;
    }

    public async Task<bool> IsServerBlockedAsync(string ruleName, CancellationToken ct = default)
    {
        var args = $"advfirewall firewall show rule name={ruleName}";
        var (_, output) = await RunNetshAsync(args, ct);
        return output.Contains(ruleName);
    }

    public async Task ResetFirewallAsync(CancellationToken ct = default)
    {
        await RunNetshAsync("advfirewall reset", ct);
    }

    public async Task<bool> CheckFirewallEnabledAsync(CancellationToken ct = default)
    {
        var (_, output) = await RunNetshAsync("advfirewall show currentprofile state", ct);
        return output.Contains("ON", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(int exitCode, string output)> RunNetshAsync(string arguments, CancellationToken ct)
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
        }, ct);
    }
}
