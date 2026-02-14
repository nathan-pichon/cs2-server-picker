using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2ServerPicker.Models;
using CS2ServerPicker.Services;

namespace CS2ServerPicker.ViewModels;

public sealed partial class ServersPageViewModel : ObservableObject
{
    private readonly IServerDataService _serverDataService;
    private readonly IFirewallService _firewallService;
    private readonly IPingService _pingService;
    private readonly AppSettings _settings;

    private List<ServerInfo> _allServers = [];
    private Dictionary<string, ClusteredServer> _clusteredServers = [];
    private CancellationTokenSource? _pingCts;
    private DispatcherTimer? _autoRefreshTimer;

    public ServersPageViewModel(
        IServerDataService serverDataService,
        IFirewallService firewallService,
        IPingService pingService,
        AppSettings settings)
    {
        _serverDataService = serverDataService;
        _firewallService = firewallService;
        _pingService = pingService;
        _settings = settings;
        _isClustered = settings.IsClustered;
    }

    public ObservableCollection<ServerItemViewModel> Servers { get; } = [];
    public ObservableCollection<ServerItemViewModel> FilteredServers { get; } = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isClustered;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedRegionFilter = "All";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isOperationInProgress;

    [ObservableProperty]
    private bool _autoRefreshEnabled;

    [ObservableProperty]
    private int _autoRefreshInterval = 60;

    [ObservableProperty]
    private string _filterMode = "All"; // All, Favorites, Blocked

    public string[] RegionFilters { get; } =
        ["All", "Europe", "North America", "South America", "Asia", "Middle East", "Africa", "Oceania"];

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedRegionFilterChanged(string value) => ApplyFilters();
    partial void OnFilterModeChanged(string value) => ApplyFilters();

    partial void OnAutoRefreshEnabledChanged(bool value)
    {
        if (value)
            StartAutoRefresh();
        else
            StopAutoRefresh();
    }

    [RelayCommand]
    private async Task LoadServersAsync()
    {
        IsLoading = true;
        StatusText = "Fetching server data from Steam...";

        var result = await _serverDataService.FetchServerDataAsync();

        if (result is null)
        {
            StatusText = "Failed to load server data. Check your internet connection.";
            IsLoading = false;
            return;
        }

        var (revision, servers) = result.Value;
        _allServers = servers;
        _clusteredServers = _serverDataService.BuildClusteredView(servers);

        // Check if server data was updated
        if (!string.IsNullOrEmpty(_settings.ServerRevision) && _settings.ServerRevision != revision)
        {
            StatusText = "Server data updated by Valve. Unblocking all servers...";
            await UnblockAllServersInternalAsync();
        }

        _settings.ServerRevision = revision;
        _settings.Save();

        PopulateServerList();
        IsLoading = false;
        StatusText = $"Loaded {Servers.Count} servers";

        await PingAllServersAsync();
    }

    [RelayCommand]
    private async Task RefreshPingsAsync()
    {
        if (IsOperationInProgress) return;
        await PingAllServersAsync();
    }

    [RelayCommand]
    private async Task ToggleClusterAsync()
    {
        if (IsOperationInProgress) return;

        StatusText = "Unblocking all servers before changing view...";
        await UnblockAllServersInternalAsync();

        IsClustered = !IsClustered;
        _settings.IsClustered = IsClustered;
        _settings.Save();

        PopulateServerList();
        StatusText = IsClustered ? "Clustered view enabled" : "Unclustered view enabled";
    }

    [RelayCommand]
    private async Task BlockSelectedAsync()
    {
        if (IsOperationInProgress) return;

        var selected = FilteredServers.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText = "No servers selected.";
            return;
        }

        IsOperationInProgress = true;
        StatusText = $"Blocking {selected.Count} server(s)...";

        foreach (var server in selected)
        {
            await _firewallService.BlockServerAsync(server.FirewallRuleName, server.CombinedAddresses);
            server.IsBlocked = true;
            server.PingStatus = PingStatus.Blocked;
            server.LatencyDisplay = "Blocked";
        }

        IsOperationInProgress = false;
        StatusText = $"Blocked {selected.Count} server(s)";
    }

    [RelayCommand]
    private async Task UnblockSelectedAsync()
    {
        if (IsOperationInProgress) return;

        var selected = FilteredServers.Where(s => s.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText = "No servers selected.";
            return;
        }

        IsOperationInProgress = true;
        StatusText = $"Unblocking {selected.Count} server(s)...";

        foreach (var server in selected)
        {
            await _firewallService.UnblockServerAsync(server.FirewallRuleName);
            server.IsBlocked = false;
            server.PingStatus = PingStatus.Unknown;
            server.LatencyDisplay = "";
        }

        IsOperationInProgress = false;
        StatusText = $"Unblocked {selected.Count} server(s)";

        await PingServersAsync(selected);
    }

    [RelayCommand]
    private async Task BlockAllAsync()
    {
        if (IsOperationInProgress) return;

        IsOperationInProgress = true;
        _pingService.CancelAll();
        StatusText = "Blocking all servers...";

        foreach (var server in Servers)
        {
            await _firewallService.BlockServerAsync(server.FirewallRuleName, server.CombinedAddresses);
            server.IsBlocked = true;
            server.PingStatus = PingStatus.Blocked;
            server.LatencyDisplay = "Blocked";
        }

        IsOperationInProgress = false;
        StatusText = "All servers blocked";
    }

    [RelayCommand]
    private async Task UnblockAllAsync()
    {
        if (IsOperationInProgress) return;

        IsOperationInProgress = true;
        _pingService.CancelAll();
        StatusText = "Unblocking all servers...";

        await UnblockAllServersInternalAsync();

        IsOperationInProgress = false;
        StatusText = "All servers unblocked";

        await PingAllServersAsync();
    }

    [RelayCommand]
    private void ToggleFavorite(ServerItemViewModel server)
    {
        server.IsFavorite = !server.IsFavorite;

        if (server.IsFavorite)
            _settings.FavoriteServers.Add(server.Name);
        else
            _settings.FavoriteServers.Remove(server.Name);

        _settings.Save();
        ApplyFilters();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var server in FilteredServers)
            server.IsSelected = true;
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var server in FilteredServers)
            server.IsSelected = false;
    }

    public async Task ApplyPresetAsync(string[] presetServers, bool blockExcept)
    {
        if (IsOperationInProgress) return;

        IsOperationInProgress = true;
        _pingService.CancelAll();

        // First unblock all
        await UnblockAllServersInternalAsync();

        var presetSet = new HashSet<string>(presetServers);

        foreach (var server in Servers)
        {
            bool shouldBlock = blockExcept
                ? !presetSet.Contains(server.Name)
                : presetSet.Contains(server.Name);

            if (shouldBlock)
            {
                await _firewallService.BlockServerAsync(server.FirewallRuleName, server.CombinedAddresses);
                server.IsBlocked = true;
                server.PingStatus = PingStatus.Blocked;
                server.LatencyDisplay = "Blocked";
            }
        }

        IsOperationInProgress = false;
        StatusText = "Preset applied";

        await PingAllServersAsync();
    }

    public async Task PingServerAsync(ServerItemViewModel server)
    {
        if (server.IsBlocked)
        {
            server.PingStatus = PingStatus.Blocked;
            server.LatencyDisplay = "Blocked";
            return;
        }

        server.IsPinging = true;
        server.PingStatus = PingStatus.Pinging;
        server.LatencyDisplay = "Pinging...";

        var result = await _pingService.PingServerAsync(server.Addresses);

        if (result.Success)
        {
            server.LatencyMs = result.LatencyMs;
            server.LatencyDisplay = $"{result.LatencyMs}ms";
            server.PingStatus = PingStatus.Success;
        }
        else if (result.TimedOut)
        {
            server.LatencyMs = -1;
            server.LatencyDisplay = "Timed out";
            server.PingStatus = PingStatus.TimedOut;
        }
        else
        {
            server.LatencyMs = -1;
            server.LatencyDisplay = "Error";
            server.PingStatus = PingStatus.Error;
        }

        server.IsPinging = false;
    }

    private async Task PingAllServersAsync()
    {
        _pingCts?.Cancel();
        _pingCts = new CancellationTokenSource();
        var ct = _pingCts.Token;

        StatusText = "Pinging all servers...";

        foreach (var server in Servers)
        {
            if (ct.IsCancellationRequested) break;

            // Check blocked status
            var isBlocked = await _firewallService.IsServerBlockedAsync(server.FirewallRuleName, ct);
            server.IsBlocked = isBlocked;

            if (isBlocked)
            {
                server.PingStatus = PingStatus.Blocked;
                server.LatencyDisplay = "Blocked";
                continue;
            }

            await PingServerAsync(server);
        }

        if (!ct.IsCancellationRequested)
            StatusText = "Ready";
    }

    private async Task PingServersAsync(IEnumerable<ServerItemViewModel> servers)
    {
        foreach (var server in servers)
        {
            await PingServerAsync(server);
        }
    }

    private async Task UnblockAllServersInternalAsync()
    {
        foreach (var server in Servers)
        {
            await _firewallService.UnblockServerAsync(server.FirewallRuleName);
            server.IsBlocked = false;
            server.PingStatus = PingStatus.Unknown;
            server.LatencyDisplay = "";
        }
    }

    private void PopulateServerList()
    {
        Servers.Clear();

        if (IsClustered)
        {
            foreach (var (name, cluster) in _clusteredServers)
            {
                Servers.Add(CreateServerItem(name, cluster.AllAddresses, cluster.Region,
                    cluster.FirewallRuleName));
            }
        }
        else
        {
            foreach (var server in _allServers)
            {
                Servers.Add(CreateServerItem(server.Name, server.RelayAddresses, server.Region,
                    server.FirewallRuleName));
            }
        }

        ApplyFilters();
    }

    private ServerItemViewModel CreateServerItem(string name, List<string> addresses, string region, string firewallRuleName)
    {
        var flagFileName = FlagResolver.GetFlagPath(name);

        return new ServerItemViewModel
        {
            Name = name,
            Region = region,
            Addresses = addresses,
            CombinedAddresses = string.Join(",", addresses),
            FirewallRuleName = firewallRuleName,
            FlagPath = flagFileName,
            IsFavorite = _settings.FavoriteServers.Contains(name)
        };
    }

    private void ApplyFilters()
    {
        FilteredServers.Clear();

        var filtered = Servers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(s =>
                s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Region.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedRegionFilter != "All")
        {
            filtered = filtered.Where(s => s.Region == SelectedRegionFilter);
        }

        filtered = FilterMode switch
        {
            "Favorites" => filtered.Where(s => s.IsFavorite),
            "Blocked" => filtered.Where(s => s.IsBlocked),
            _ => filtered
        };

        foreach (var server in filtered)
            FilteredServers.Add(server);
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();

        if (AutoRefreshInterval <= 0) return;

        _autoRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(AutoRefreshInterval)
        };
        _autoRefreshTimer.Tick += async (_, _) => await PingAllServersAsync();
        _autoRefreshTimer.Start();
    }

    private void StopAutoRefresh()
    {
        _autoRefreshTimer?.Stop();
        _autoRefreshTimer = null;
    }

    public void Cleanup()
    {
        StopAutoRefresh();
        _pingCts?.Cancel();
        _pingService.CancelAll();
    }
}

public static class FlagResolver
{
    public static string GetFlagPath(string serverName)
    {
        // Try exact match with the flag files
        var safeName = serverName.Replace("/", "").Replace("\\", "");
        return $"pack://application:,,,/Assets/Flags/{safeName}.png";
    }
}
